import { create } from 'zustand'
import { approvalApi, authApi } from './api'
import { onLive } from './live'
import { useAuthStore } from '../store'
import { naira } from '../utils/format'

// Requests waiting for this manager's answer, kept up to date by the live connection. Requests
// from the manager's own shop pop up with a sound ("loud"); Admins also see every other shop's,
// quietly, in the bell and on the Approvals page.
export const useApprovalsStore = create((set, get) => ({
  waiting: [],             // oldest first; never the manager's own requests
  homeStoreId: undefined,  // undefined until loaded
  dismissed: [],           // pop-ups put away with "Later" (they stay in the list)

  load: async () => {
    try {
      const [list, me] = await Promise.all([
        approvalApi.list(true),
        get().homeStoreId === undefined ? authApi.getMe() : null,
      ])
      const myId = useAuthStore.getState().user?.id
      set({
        waiting: list.data.filter(r => r.requestedById !== myId),
        ...(me && { homeStoreId: me.data.storeId ?? null }),
      })
    } catch { /* keeps what it has; the next (re)connection loads it again */ }
  },

  remove: id => set(s => ({ waiting: s.waiting.filter(r => r.id !== id) })),
  dismiss: id => set(s => ({ dismissed: [...s.dismissed, id] })),
}))

export const isLoud = r => r.storeId === useApprovalsStore.getState().homeStoreId

// Requests that have chimed. Each chimes once: an Admin whose home shop asked is in both the
// shop's group and the Admins', so hears it twice. This isn't the waiting list, which a reload
// after reconnecting can fill before the request's own message arrives.
const chimed = new Set()

// Called by the back office's layout for staff who can approve; the first call starts listening
let watching = false
export function watchApprovals() {
  if (watching) return
  watching = true
  onLive('connected', () => useApprovalsStore.getState().load()) // catch up on anything missed
  onLive('ApprovalRequested', r => {
    if (r.requestedById === useAuthStore.getState().user?.id) return
    useApprovalsStore.setState(s => ({ waiting: [...s.waiting.filter(x => x.id !== r.id), r] }))
    if (isLoud(r) && !chimed.has(r.id)) {
      chimed.add(r.id)
      alertManager(r)
    }
  })
  onLive('ApprovalUpdated', r => {
    if (r.status !== 'Waiting') useApprovalsStore.getState().remove(r.id)
  })
}

// Someone else signing in on this browser starts with an empty list
useAuthStore.subscribe(s => {
  if (!s.isAuthenticated) {
    useApprovalsStore.setState({ waiting: [], homeStoreId: undefined, dismissed: [] })
    chimed.clear()
  }
})

// ── The alert: a chime, a buzz on phones, and a system notification when the tab is hidden ──

let audio
// Browsers only play sound once the page has been tapped or clicked, so the first tap readies it
const readySound = () => {
  try {
    audio ??= new (window.AudioContext || window.webkitAudioContext)()
    audio.resume?.()
  } catch { /* no sound on this device */ }
}
window.addEventListener('pointerdown', readySound, { once: true })

export function chime() {
  readySound()
  if (!audio) return
  const start = audio.currentTime + 0.05
  ;[880, 1175, 1568].forEach((freq, i) => {
    const t = start + i * 0.15
    const osc = audio.createOscillator()
    const gain = audio.createGain()
    osc.frequency.value = freq
    gain.gain.setValueAtTime(0.0001, t)
    gain.gain.exponentialRampToValueAtTime(0.25, t + 0.02)
    gain.gain.exponentialRampToValueAtTime(0.0001, t + 0.3)
    osc.connect(gain).connect(audio.destination)
    osc.start(t)
    osc.stop(t + 0.32)
  })
}

function alertManager(r) {
  chime()
  navigator.vibrate?.([200, 100, 200])
  if (document.hidden && 'Notification' in window && Notification.permission === 'granted') {
    try {
      const n = new Notification('Approval needed', {
        body: `${r.requestedByName} at ${r.storeName}: ${naira(r.total)} (${r.reason})`,
        tag: `approval-${r.id}`,
      })
      n.onclick = () => { window.focus(); n.close() }
    } catch { /* phone browsers only show these for installed apps */ }
  }
}
