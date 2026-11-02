import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { format } from 'date-fns'
import { BellRing, Volume2 } from 'lucide-react'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { ApprovalAnswer, ApprovalDetails, LiveDot, timeLeft, useNow } from '../../components/layout/ApprovalAlerts'
import { EmptyState, Spinner } from '../../components/ui'
import { approvalApi } from '../../services/api'
import { chime, useApprovalsStore } from '../../services/approvals'
import { onLive } from '../../services/live'
import { naira, utcDate } from '../../utils/format'

const STATUS = {
  Waiting:   { label: 'Waiting',   className: 'badge-yellow' },
  Approved:  { label: 'Approved',  className: 'badge-green' },
  Used:      { label: 'Approved, sold', className: 'badge-green' },
  Declined:  { label: 'Declined',  className: 'badge-red' },
  Cancelled: { label: 'Cancelled', className: 'badge-gray' },
  Expired:   { label: 'Expired',   className: 'badge-gray' },
}

// Managers answer the tills' discount and price-change requests here (or in the pop-up any
// admin page shows), and see the latest 50
export default function AdminApprovals() {
  const now = useNow()
  const waiting = useApprovalsStore(s => s.waiting).filter(r => timeLeft(r, now) > 0)
  const [recent, setRecent] = useState(null)
  const [notify, setNotify] = useState(() => ('Notification' in window ? Notification.permission : 'unsupported'))

  useEffect(() => {
    let current = true
    const load = () => approvalApi.list(false).then(r => { if (current) setRecent(r.data) }).catch(() => {})
    load()
    const stop = [onLive('ApprovalRequested', load), onLive('ApprovalUpdated', load), onLive('connected', load)]
    return () => { current = false; stop.forEach(s => s()) }
  }, [])

  const askToNotify = () => Notification.requestPermission().then(setNotify)

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-5xl">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Approvals</h1>
            <p className="text-surface-400 text-sm mt-0.5">Discounts and price changes the tills are asking a manager to approve</p>
          </div>
          <LiveDot />
        </div>

        <div className="card p-4 text-sm text-surface-600 space-y-2">
          <p>
            Keep the back office open on your phone or computer: a request from your shop pops up on any page with a chime.
            Approve with your <Link to="/account/profile" className="text-brand-600 hover:underline">approval PIN</Link>.
          </p>
          <div className="flex flex-wrap gap-2">
            <button type="button" className="btn-secondary text-xs" onClick={chime}>
              <Volume2 className="w-3.5 h-3.5" /> Test the chime
            </button>
            {notify === 'default' && (
              <button type="button" className="btn-secondary text-xs" onClick={askToNotify}>
                <BellRing className="w-3.5 h-3.5" /> Also notify me when this tab is in the background
              </button>
            )}
            {notify === 'granted' && <span className="text-xs text-emerald-600 self-center">Background notifications are on for this browser.</span>}
          </div>
        </div>

        <section className="space-y-3">
          <h2 className="font-semibold text-surface-900">Waiting {waiting.length > 0 && <span className="text-surface-400 font-normal">({waiting.length})</span>}</h2>
          {waiting.length === 0 ? (
            <div className="card"><EmptyState icon={BellRing} title="Nothing waiting" description="Requests from the tills appear here as they're made." /></div>
          ) : (
            <div className="grid md:grid-cols-2 gap-4">
              {waiting.map(r => (
                <div key={r.id} className="card p-4 space-y-4">
                  <ApprovalDetails r={r} now={now} />
                  <ApprovalAnswer r={r} />
                </div>
              ))}
            </div>
          )}
        </section>

        <section className="space-y-3">
          <h2 className="font-semibold text-surface-900">Recent</h2>
          <div className="card divide-y divide-surface-100">
            {recent === null ? (
              <div className="flex justify-center py-10"><Spinner /></div>
            ) : recent.length === 0 ? (
              <p className="text-center text-sm text-surface-400 py-10">No requests yet</p>
            ) : recent.map(r => {
              const s = STATUS[r.status] ?? STATUS.Cancelled
              return (
                <div key={r.id} className="p-4 flex flex-wrap items-start justify-between gap-x-4 gap-y-1 text-sm">
                  <div className="min-w-0">
                    <p className="font-medium text-surface-900">
                      {naira(r.total)} <span className="text-surface-400 font-normal">· {naira(r.normalSubTotal - r.total)} off</span>
                    </p>
                    <p className="text-xs text-surface-500">
                      {format(utcDate(r.createdAt), 'd MMM, HH:mm')} · {r.requestedByName} at {r.storeName} · {r.reason}
                    </p>
                    {r.decisionNote && <p className="text-xs text-surface-500 mt-0.5">“{r.decisionNote}”</p>}
                  </div>
                  <div className="text-right">
                    <span className={s.className}>{s.label}</span>
                    <p className="text-xs text-surface-400 mt-1">
                      {r.decidedByName && `by ${r.decidedByName}`}
                      {r.orderNumber && <> · <Link to={`/admin/pos/sales/${r.orderId}`} className="text-brand-600 hover:underline">{r.orderNumber}</Link></>}
                    </p>
                  </div>
                </div>
              )
            })}
          </div>
        </section>
      </div>
    </AdminLayout>
  )
}
