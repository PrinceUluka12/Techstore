import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import toast from 'react-hot-toast'
import { authApi, posApi, storeApi } from '../../services/api'
import { useAuthStore } from '../../store'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Spinner } from '../../components/ui'
import { showApiError } from '../../utils/validation'
import { naira } from './AdminPos'

const METHOD_LABELS = { Cash: 'Cash in till', Card: 'Card (POS terminal)', BankTransfer: 'Bank transfer', USSD: 'USSD', Other: 'Other' }

// Days run in Nigeria time (UTC+1, no daylight saving), like the API, so times are shown
// in Nigeria time too, wherever the viewer is
const todayInNigeria = () => new Date(Date.now() + 3600_000).toISOString().slice(0, 10)
const timeOf = (d) => new Date(d.endsWith('Z') ? d : `${d}Z`)
  .toLocaleTimeString('en-GB', { timeZone: 'Africa/Lagos', hour: '2-digit', minute: '2-digit' })

export default function AdminCashUp() {
  const { isAdmin } = useAuthStore()
  const admin = isAdmin()
  const [date, setDate] = useState(todayInNigeria())
  const [stores, setStores] = useState([])
  const [storeId, setStoreId] = useState(null)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [noShop, setNoShop] = useState(false)

  useEffect(() => {
    Promise.all([authApi.getMe(), storeApi.getAll()]).then(([me, s]) => {
      const active = s.data.filter(x => x.isActive)
      setStores(active)
      const id = me.data.storeId ?? (admin ? active.find(x => x.sendsOnlineOrders)?.id ?? active[0]?.id : null)
      if (id) setStoreId(id); else { setNoShop(true); setLoading(false) }
    }).catch(() => toast.error('Failed to load shops'))
  }, [])

  useEffect(() => {
    if (!storeId || !date) return
    setLoading(true)
    posApi.cashUp({ date, storeId })
      .then(r => setData(r.data))
      .catch(err => { setData(null); showApiError(err, null, 'Failed to load the cash-up') })
      .finally(() => setLoading(false))
  }, [storeId, date])

  // Cashiers only get their own sales back; managers and admins get the whole shop
  const own = data?.ownSalesOnly

  if (noShop) return (
    <AdminLayout>
      <div className="card p-8 max-w-lg text-center space-y-2">
        <h1 className="font-display text-xl font-semibold">No home shop</h1>
        <p className="text-surface-500 text-sm">You need a home shop to see its cash-up. An admin can set it on the Customers page.</p>
      </div>
    </AdminLayout>
  )

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">End-of-day Cash-up</h1>
            <p className="text-surface-400 text-sm mt-0.5">
              {own
                ? `Your own sales at ${data.shop.name}, by payment method. A manager sees the whole shop.`
                : `In-store takings for ${data?.shop.name ?? '…'}, by payment method and cashier`}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {admin && (
              <select className="input w-auto text-sm" value={storeId ?? ''} aria-label="Shop"
                onChange={e => setStoreId(Number(e.target.value))}>
                {stores.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            )}
            <input type="date" className="input w-auto text-sm" value={date} max={todayInNigeria()} aria-label="Day"
              onChange={e => setDate(e.target.value)} />
          </div>
        </div>

        {loading ? <div className="flex justify-center py-16"><Spinner size="lg" /></div> : !data ? null : (
          <>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="card p-4">
                <p className="text-xs text-surface-400">Total sales</p>
                <p className="font-display text-2xl font-bold mt-1">{naira(data.total)}</p>
                <p className="text-xs text-surface-500 mt-1">{data.salesCount} sale{data.salesCount === 1 ? '' : 's'}
                  {data.cancelledCount > 0 && ` · ${data.cancelledCount} cancelled (not counted)`}</p>
              </div>
              {data.byMethod.map(m => (
                <div key={m.method} className="card p-4">
                  <p className="text-xs text-surface-400">{METHOD_LABELS[m.method] ?? m.method}</p>
                  <p className="font-display text-xl font-semibold mt-1">{naira(m.amount)}</p>
                  <p className="text-xs text-surface-500 mt-1">{m.count} payment{m.count === 1 ? '' : 's'}</p>
                </div>
              ))}
            </div>

            <div className="grid sm:grid-cols-3 gap-4 text-sm">
              <div className="card p-4 flex justify-between"><span className="text-surface-500">Cash handed over</span><span className="font-medium">{naira(data.cashTendered)}</span></div>
              <div className="card p-4 flex justify-between"><span className="text-surface-500">Change given</span><span className="font-medium">{naira(data.changeGiven)}</span></div>
              <div className="card p-4 flex justify-between"><span className="text-surface-500">Discounts given</span><span className="font-medium">{naira(data.discountTotal)}</span></div>
            </div>

            {!own && <div className="card p-4">
              <h2 className="font-semibold text-surface-900 mb-3">By cashier</h2>
              {data.byCashier.length === 0 ? <p className="text-sm text-surface-400">No sales on this day.</p> : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-xs uppercase tracking-wide text-surface-400 border-b border-surface-100">
                        <th className="py-2">Cashier</th><th className="py-2">Sales</th>
                        {Object.keys(METHOD_LABELS).map(m => <th key={m} className="py-2 text-right">{m === 'Cash' ? 'Cash' : METHOD_LABELS[m].replace(' (POS terminal)', '')}</th>)}
                        <th className="py-2 text-right">Total</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.byCashier.map(c => (
                        <tr key={c.cashierId ?? 0} className="border-b border-surface-50">
                          <td className="py-2 font-medium">{c.cashierName}</td>
                          <td className="py-2">{c.salesCount}</td>
                          {Object.keys(METHOD_LABELS).map(m => (
                            <td key={m} className="py-2 text-right text-surface-600">
                              {(() => { const x = c.byMethod.find(b => b.method === m); return x ? naira(x.amount) : '—' })()}
                            </td>
                          ))}
                          <td className="py-2 text-right font-semibold">{naira(c.total)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>}

            <div className="card p-4">
              <h2 className="font-semibold text-surface-900 mb-3">{own ? 'Your sales' : 'Sales'}</h2>
              {data.sales.length === 0 ? <p className="text-sm text-surface-400">No sales on this day.</p> : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-xs uppercase tracking-wide text-surface-400 border-b border-surface-100">
                        <th className="py-2">Time</th><th className="py-2">Receipt</th>{!own && <th className="py-2">Cashier</th>}
                        <th className="py-2">Paid by</th><th className="py-2 text-right">Total</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.sales.map(s => {
                        const cancelled = s.status === 'Cancelled' || s.status === 'Refunded'
                        return (
                          <tr key={s.id} className={`border-b border-surface-50 ${cancelled ? 'text-surface-400 line-through' : ''}`}>
                            <td className="py-2">{timeOf(s.createdAt)}</td>
                            <td className="py-2"><Link to={`/admin/pos/sales/${s.id}`} className="font-mono text-xs text-brand-600 hover:underline">{s.orderNumber}</Link></td>
                            {!own && <td className="py-2">{s.cashierName}</td>}
                            <td className="py-2">{s.paymentMethod === 'Split' ? 'Split' : (METHOD_LABELS[s.paymentMethod] ?? s.paymentMethod).replace(' in till', '')}</td>
                            <td className="py-2 text-right font-medium">{naira(s.total)}</td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </AdminLayout>
  )
}
