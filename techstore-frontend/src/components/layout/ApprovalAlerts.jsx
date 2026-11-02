import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { clsx } from 'clsx'
import { Bell, ShieldCheck, X } from 'lucide-react'
import toast from 'react-hot-toast'
import { approvalApi } from '../../services/api'
import { useApprovalsStore, isLoud } from '../../services/approvals'
import { useLiveStore } from '../../services/live'
import { Spinner } from '../ui'
import { naira, utcDate } from '../../utils/format'
import { PIN_RE } from '../../utils/validation'

// The time now, updated every second (for the countdowns)
export function useNow() {
  const [now, setNow] = useState(Date.now())
  useEffect(() => {
    const t = setInterval(() => setNow(Date.now()), 1000)
    return () => clearInterval(t)
  }, [])
  return now
}

export const timeLeft = (r, now) => Math.max(0, utcDate(r.expiresAt).getTime() - now)
export const mmss = ms => `${Math.floor(ms / 60000)}:${String(Math.floor(ms / 1000) % 60).padStart(2, '0')}`

// ── What the till is asking for ───────────────────────────────────────────────
export function ApprovalDetails({ r, now }) {
  const discounted = r.discountAmount > 0
  const repriced = r.subTotal !== r.normalSubTotal
  return (
    <div className="space-y-3 text-sm">
      <p className="text-surface-500">
        <strong className="text-surface-800">{r.requestedByName}</strong> at <strong className="text-surface-800">{r.storeName}</strong>
        {now != null && r.status === 'Waiting' && <> · <span className="tabular-nums">{mmss(timeLeft(r, now))}</span> left</>}
      </p>
      <ul className="divide-y divide-surface-100 border-y border-surface-100">
        {r.items.map(i => (
          <li key={i.productId + '-' + i.unitPrice} className="py-1.5 flex justify-between gap-3">
            <span className="min-w-0 truncate">{i.quantity > 1 && `${i.quantity} × `}{i.productName}</span>
            <span className="text-right flex-shrink-0">
              {i.unitPrice !== i.normalPrice && <span className="text-surface-400 line-through mr-1.5">{naira(i.normalPrice * i.quantity)}</span>}
              <span className={i.unitPrice !== i.normalPrice ? 'text-amber-700 font-medium' : ''}>{naira(i.unitPrice * i.quantity)}</span>
            </span>
          </li>
        ))}
      </ul>
      <div className="space-y-0.5">
        {repriced && (
          <div className="flex justify-between text-surface-500"><span>Normal price</span><span>{naira(r.normalSubTotal)}</span></div>
        )}
        {discounted && <div className="flex justify-between text-emerald-600"><span>Discount</span><span>–{naira(r.discountAmount)}</span></div>}
        <div className="flex justify-between font-semibold"><span>Customer pays</span><span>{naira(r.total)}</span></div>
        {repriced && discounted && (
          <div className="flex justify-between text-surface-500 text-xs"><span>Off the normal price in all</span><span>{naira(r.normalSubTotal - r.total)}</span></div>
        )}
      </div>
      <p className="bg-surface-50 rounded-lg px-3 py-2"><span className="text-surface-400">Reason: </span>{r.reason}</p>
    </div>
  )
}

// ── Approve with your PIN, or decline with a note ─────────────────────────────
export function ApprovalAnswer({ r, onDone }) {
  const [pin, setPin] = useState('')
  const [declining, setDeclining] = useState(false)
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const remove = useApprovalsStore(s => s.remove)

  const answer = async (call, done) => {
    setBusy(true); setError('')
    try {
      await call()
      toast.success(done)
      remove(r.id)
      onDone?.()
    } catch (err) {
      const message = err.response?.data?.message ?? 'Something went wrong. Try again.'
      // Someone else answered first, it was cancelled or it ran out: it's no longer waiting
      if (err.response?.data?.code === 'answered') { toast(message); remove(r.id); onDone?.() }
      else setError(message)
    } finally { setBusy(false) }
  }

  const approve = e => {
    e.preventDefault()
    if (!PIN_RE.test(pin)) return setError('Your PIN is 4–6 digits')
    answer(() => approvalApi.approve(r.id, pin), 'Approved. The till can finish the sale.')
  }

  return declining ? (
    <div className="space-y-3">
      <input className="input" maxLength={200} autoFocus aria-label="Note for the cashier"
        placeholder="Note for the cashier (optional), e.g. ₦3,000 off at most"
        value={note} onChange={e => setNote(e.target.value)} />
      {error && <p className="text-red-500 text-sm">{error}</p>}
      <div className="flex gap-2 justify-end">
        <button type="button" className="btn-secondary" onClick={() => setDeclining(false)} disabled={busy}>Back</button>
        <button type="button" className="btn-danger" disabled={busy}
          onClick={() => answer(() => approvalApi.decline(r.id, note.trim() || null), 'Declined. The cashier has been told.')}>
          {busy ? <Spinner size="sm" /> : 'Decline'}
        </button>
      </div>
    </div>
  ) : (
    <form onSubmit={approve} noValidate className="space-y-3">
      <div className="flex gap-2">
        <input className="input flex-1 tracking-[0.4em] font-mono" type="password" inputMode="numeric" maxLength={6}
          autoComplete="off" placeholder="Your PIN" aria-label="Your approval PIN" value={pin}
          onChange={e => setPin(e.target.value.replace(/\D/g, ''))} />
        <button type="submit" className="btn-primary" disabled={busy}>{busy ? <Spinner size="sm" /> : 'Approve'}</button>
      </div>
      {error && <p className="text-red-500 text-sm">{error}</p>}
      <div className="flex justify-between text-xs">
        <button type="button" className="text-red-500 hover:underline" onClick={() => { setDeclining(true); setError('') }}>Decline instead</button>
        <Link to="/account/profile" className="text-surface-400 hover:underline">Set or change your PIN</Link>
      </div>
    </form>
  )
}

// ── Header: the live dot and the bell ─────────────────────────────────────────
export function LiveDot() {
  const status = useLiveStore(s => s.status)
  const label = { live: 'Live', connecting: 'Connecting…', reconnecting: 'Reconnecting…', off: 'Offline' }[status]
  return (
    <span className="hidden sm:flex items-center gap-1.5 text-xs text-surface-400" title={`Live alerts: ${label}`}>
      <span className={clsx('w-2 h-2 rounded-full', status === 'live' ? 'bg-emerald-500' : status === 'off' ? 'bg-surface-300' : 'bg-amber-400 animate-pulse')} />
      {label}
    </span>
  )
}

export function ApprovalBell() {
  const now = useNow()
  const count = useApprovalsStore(s => s.waiting.filter(r => timeLeft(r, now) > 0).length)
  return (
    <Link to="/admin/approvals" className="btn-icon relative" aria-label={`Approvals${count ? `: ${count} waiting` : ''}`} title="Approvals">
      <Bell className="w-4 h-4" />
      {count > 0 && (
        <span className="absolute -top-0.5 -right-0.5 min-w-[1.1rem] h-[1.1rem] px-1 rounded-full bg-red-500 text-white text-[10px] font-semibold flex items-center justify-center">
          {count}
        </span>
      )}
    </Link>
  )
}

// ── The pop-up: the oldest request from the manager's own shop ────────────────
export function ApprovalPopup() {
  const now = useNow()
  const { pathname } = useLocation()
  const { waiting, dismissed, dismiss } = useApprovalsStore()
  const r = waiting.find(x => isLoud(x) && !dismissed.includes(x.id) && timeLeft(x, now) > 0)
  if (!r || pathname === '/admin/approvals') return null // that page shows them all

  const more = waiting.filter(x => x.id !== r.id && timeLeft(x, now) > 0).length
  return (
    <div role="alertdialog" aria-label="Approval needed"
      className="fixed z-40 inset-x-0 bottom-0 sm:inset-x-auto sm:bottom-4 sm:right-4 sm:w-[24rem] bg-white shadow-2xl border border-surface-100 rounded-t-2xl sm:rounded-2xl animate-fade-up max-h-[85dvh] overflow-y-auto">
      <div className="flex items-center justify-between px-4 pt-4">
        <h2 className="font-display font-semibold flex items-center gap-2">
          <ShieldCheck className="w-4 h-4 text-amber-500" /> Approval needed
        </h2>
        <button className="btn-icon" onClick={() => dismiss(r.id)} aria-label="Later" title="Later (it stays under Approvals)">
          <X className="w-4 h-4" />
        </button>
      </div>
      <div className="p-4 space-y-4">
        <ApprovalDetails r={r} now={now} />
        <ApprovalAnswer key={r.id} r={r} />
        {more > 0 && (
          <Link to="/admin/approvals" className="block text-center text-xs text-brand-600 hover:underline">
            {more} more waiting
          </Link>
        )}
      </div>
    </div>
  )
}
