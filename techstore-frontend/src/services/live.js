import { create } from 'zustand'
import { authApi } from './api'
import { useAuthStore } from '../store'

// The back office's live connection to the API (NotificationsHub on the server), for alerts such
// as a till asking for a manager's approval. One per browser tab, kept while staff move between
// admin pages. The SignalR library is only downloaded when it first connects, so shoppers never
// load it.

// off | connecting | live | reconnecting
export const useLiveStore = create(() => ({ status: 'off' }))
const setStatus = status => useLiveStore.setState({ status })

// Next to the API: VITE_API_URL is ".../api", the hub is ".../hubs/notifications"
const hubUrl = () => (import.meta.env.VITE_API_URL ?? '/api').replace(/\/api\/?$/, '') + '/hubs/notifications'

// The access token lasts 15 minutes and the server closes the connection when it runs out. Before
// (re)connecting with one that has run out, a small API call renews it (api.js does the renewing,
// so two renewals never race).
const expired = token => {
  try {
    const { exp } = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
    return exp * 1000 <= Date.now() + 5000
  } catch { return true }
}
const freshToken = async () => {
  if (expired(localStorage.getItem('access_token') ?? '')) await authApi.getMe().catch(() => {})
  return localStorage.getItem('access_token') ?? ''
}

// Listeners by message name. 'connected' fires on every (re)connection, so a page can reload what
// it may have missed while the connection was down.
const listeners = new Map()
const emit = (event, data) => listeners.get(event)?.forEach(fn => fn(data))
export function onLive(event, fn) {
  if (!listeners.has(event)) listeners.set(event, new Set())
  listeners.get(event).add(fn)
  return () => listeners.get(event).delete(fn)
}

const MESSAGES = ['ApprovalRequested', 'ApprovalUpdated']
let connection = null
let wanted = false
let retryTimer

export async function startLive() {
  if (wanted) return
  wanted = true
  const { HubConnectionBuilder, HubConnectionState, LogLevel } = await import('@microsoft/signalr')
  if (!wanted || connection) return // stopped while the library loaded

  connection = new HubConnectionBuilder()
    .withUrl(hubUrl(), { accessTokenFactory: freshToken })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.None)
    .build()
  MESSAGES.forEach(m => connection.on(m, data => emit(m, data)))
  connection.onreconnecting(() => setStatus('reconnecting'))
  connection.onreconnected(() => { setStatus('live'); emit('connected') })
  // Given up reconnecting, or closed by the server when the token ran out: start again
  connection.onclose(() => { setStatus('off'); if (wanted) retry(1000) })

  const connect = async () => {
    if (!wanted || connection?.state !== HubConnectionState.Disconnected) return
    if (!localStorage.getItem('access_token')) return stopLive() // signed out
    setStatus('connecting')
    try {
      await connection.start()
      setStatus('live')
      emit('connected')
    } catch {
      setStatus('off')
      retry(15000)
    }
  }
  const retry = ms => { clearTimeout(retryTimer); retryTimer = setTimeout(connect, ms) }
  await connect()
}

// Signing out anywhere (the shop's menu too) ends it
useAuthStore.subscribe(s => { if (!s.isAuthenticated && wanted) stopLive() })

export async function stopLive() {
  wanted = false
  clearTimeout(retryTimer)
  const c = connection
  connection = null
  setStatus('off')
  await c?.stop().catch(() => {})
}
