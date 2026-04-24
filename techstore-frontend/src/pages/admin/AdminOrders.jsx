import { useEffect, useState } from 'react'
import { Eye, Clock, User, ArrowRight, CheckCircle, XCircle, Package } from 'lucide-react'
import { orderApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, StatusBadge, Spinner, Pagination } from '../../components/ui'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const ORDER_STATUSES = ['Pending','Confirmed','Processing','Shipped','Delivered','Cancelled','Refunded']

const STATUS_COLORS = {
  Pending:    'bg-amber-100 text-amber-700 border-amber-200',
  Confirmed:  'bg-blue-100 text-blue-700 border-blue-200',
  Processing: 'bg-violet-100 text-violet-700 border-violet-200',
  Shipped:    'bg-cyan-100 text-cyan-700 border-cyan-200',
  Delivered:  'bg-emerald-100 text-emerald-700 border-emerald-200',
  Cancelled:  'bg-red-100 text-red-700 border-red-200',
  Refunded:   'bg-surface-100 text-surface-600 border-surface-200',
}

// ── Status History Timeline ───────────────────────────────────────────────────
function StatusTimeline({ logs, loading }) {
  if (loading) return <div className="flex justify-center py-10"><Spinner /></div>

  if (!logs.length) return (
    <div className="text-center py-10 text-surface-400 text-sm">No history recorded yet.</div>
  )

  return (
    <div className="relative">
      {/* Vertical line */}
      <div className="absolute left-4 top-4 bottom-4 w-px bg-surface-200" />

      <div className="space-y-0">
        {logs.map((log, i) => {
          const isFirst = i === 0
          const isLast  = i === logs.length - 1
          const isSystem = log.changedByEmail === 'system'

          return (
            <div key={log.id} className="relative flex gap-4 pb-6 last:pb-0">
              {/* Dot */}
              <div className={`relative z-10 w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 border-2
                ${isLast
                  ? 'bg-brand-500 border-brand-500'
                  : 'bg-white border-surface-300'}`}>
                {isSystem
                  ? <Package className={`w-3.5 h-3.5 ${isLast ? 'text-white' : 'text-surface-400'}`} />
                  : <User className={`w-3.5 h-3.5 ${isLast ? 'text-white' : 'text-surface-400'}`} />}
              </div>

              {/* Content */}
              <div className="flex-1 min-w-0 pt-0.5">
                <div className="flex flex-wrap items-center gap-2 mb-1">
                  {/* From → To */}
                  {log.fromStatus === log.toStatus ? (
                    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border
                      ${STATUS_COLORS[log.toStatus] ?? 'bg-surface-100 text-surface-600 border-surface-200'}`}>
                      {log.toStatus}
                    </span>
                  ) : (
                    <div className="flex items-center gap-1.5">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border
                        ${STATUS_COLORS[log.fromStatus] ?? 'bg-surface-100 text-surface-600 border-surface-200'}`}>
                        {log.fromStatus}
                      </span>
                      <ArrowRight className="w-3 h-3 text-surface-400" />
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border
                        ${STATUS_COLORS[log.toStatus] ?? 'bg-surface-100 text-surface-600 border-surface-200'}`}>
                        {log.toStatus}
                      </span>
                    </div>
                  )}
                </div>

                {/* Who + when */}
                <div className="flex flex-wrap items-center gap-1.5 text-xs text-surface-500">
                  <span className="font-medium text-surface-700">
                    {isSystem ? 'System' : log.changedByName}
                  </span>
                  {!isSystem && (
                    <span className="text-surface-400">({log.changedByEmail})</span>
                  )}
                  <span className="text-surface-300">·</span>
                  <span className="flex items-center gap-1">
                    <Clock className="w-3 h-3" />
                    {format(new Date(log.changedAt), 'MMM d, yyyy · h:mm a')}
                  </span>
                </div>

                {/* Note */}
                {log.note && (
                  <p className="mt-1.5 text-xs text-surface-600 bg-surface-50 rounded-lg px-3 py-2 border border-surface-100">
                    "{log.note}"
                  </p>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

// ── Order Detail Modal ────────────────────────────────────────────────────────
function OrderDetailModal({ open, onClose, orderId, onUpdated }) {
  const [order, setOrder]   = useState(null)
  const [logs, setLogs]     = useState([])
  const [loading, setLoading]         = useState(false)
  const [logsLoading, setLogsLoading] = useState(false)
  const [updating, setUpdating]       = useState(false)
  const [activeTab, setActiveTab]     = useState('details')

  const loadOrder = () => {
    if (!orderId) return
    setLoading(true)
    orderApi.getById(orderId).then(r => setOrder(r.data)).finally(() => setLoading(false))
  }

  const loadHistory = () => {
    if (!orderId) return
    setLogsLoading(true)
    orderApi.getHistory(orderId).then(r => setLogs(r.data ?? [])).catch(() => setLogs([])).finally(() => setLogsLoading(false))
  }

  useEffect(() => {
    if (!open || !orderId) return
    loadOrder()
    loadHistory()
    setActiveTab('details')
  }, [orderId, open])

  const updateStatus = async (status) => {
    setUpdating(true)
    try {
      await orderApi.updateStatus(orderId, { status })
      toast.success(`Status updated → ${status}`)
      loadOrder()
      loadHistory()
      onUpdated()
    } catch { toast.error('Failed to update status') }
    finally { setUpdating(false) }
  }

  const tabs = [
    { id: 'details', label: 'Order Details' },
    { id: 'history', label: `History${logs.length ? ` (${logs.length})` : ''}` },
  ]

  return (
    <Modal open={open} onClose={onClose}
      title={order ? `Order ${order.orderNumber}` : 'Order Details'}
      size="xl">
      {loading ? (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      ) : order ? (
        <div className="space-y-5">

          {/* Tab switcher */}
          <div className="flex border-b border-surface-100 -mx-6 px-6">
            {tabs.map(t => (
              <button key={t.id} onClick={() => setActiveTab(t.id)}
                className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors -mb-px
                  ${activeTab === t.id
                    ? 'border-brand-500 text-brand-600'
                    : 'border-transparent text-surface-500 hover:text-surface-900'}`}>
                {t.label}
              </button>
            ))}
          </div>

          {/* ── Details tab ── */}
          {activeTab === 'details' && (
            <>
              {/* Status row */}
              <div className="flex flex-wrap items-center gap-3">
                <StatusBadge status={order.status} />
                <StatusBadge status={order.paymentStatus} />
                <span className="text-sm text-surface-400">
                  {format(new Date(order.createdAt), 'MMM d, yyyy · h:mm a')}
                </span>
                {order.paymentMethodDetails && (
                  <span className="text-xs text-surface-500 bg-surface-100 px-2.5 py-1 rounded-full">
                    {order.paymentMethodDetails}
                  </span>
                )}
              </div>

              {/* Status update buttons */}
              <div className="bg-surface-50 rounded-xl p-4">
                <p className="text-xs font-semibold text-surface-500 uppercase tracking-wider mb-3">
                  Update Status
                </p>
                <div className="flex flex-wrap gap-2">
                  {ORDER_STATUSES.filter(s => s !== order.status).map(s => (
                    <button key={s} onClick={() => updateStatus(s)} disabled={updating}
                      className={`text-xs px-3 py-1.5 rounded-lg font-medium border transition-colors
                        ${STATUS_COLORS[s] ?? 'bg-surface-100 text-surface-600 border-surface-200'}
                        hover:opacity-80 disabled:opacity-50`}>
                      {updating ? '…' : `→ ${s}`}
                    </button>
                  ))}
                </div>
              </div>

              <div className="grid sm:grid-cols-2 gap-5">
                {/* Customer + address */}
                <div>
                  <p className="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-2">Customer</p>
                  <p className="font-medium text-surface-900">{order.customerName}</p>
                  <p className="text-sm text-surface-500">{order.customerEmail}</p>

                  <p className="text-xs font-semibold text-surface-400 uppercase tracking-wider mt-4 mb-2">Shipping Address</p>
                  <address className="text-sm text-surface-600 not-italic leading-relaxed">
                    {order.shippingAddress}<br />
                    {order.shippingCity}, {order.shippingProvince} {order.shippingPostalCode}<br />
                    {order.shippingCountry}
                  </address>
                </div>

                {/* Totals */}
                <div>
                  <p className="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-2">Order Total</p>
                  <div className="space-y-1.5 text-sm">
                    <div className="flex justify-between text-surface-500"><span>Subtotal</span><span>₦{order.subTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></div>
                    <div className="flex justify-between text-surface-500"><span>VAT (7.5%)</span><span>₦{order.tax.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></div>
                    <div className="flex justify-between text-surface-500"><span>Shipping</span><span>₦{order.shippingCost.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></div>
                    <div className="flex justify-between font-bold text-surface-900 border-t border-surface-100 pt-2 mt-2">
                      <span>Total</span><span>₦{order.total.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Items */}
              <div>
                <p className="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-3">
                  Items ({order.items.length})
                </p>
                <div className="rounded-xl border border-surface-100 overflow-hidden">
                  {order.items.map((item, i) => (
                    <div key={item.id}
                      className={`flex items-center gap-3 p-3 ${i > 0 ? 'border-t border-surface-100' : ''}`}>
                      <div className="w-10 h-10 rounded-lg bg-white border border-surface-100 flex-shrink-0 overflow-hidden">
                        {item.productImageUrl
                          ? <img src={item.productImageUrl} alt={item.productName} className="w-full h-full object-contain p-1" />
                          : <div className="w-full h-full flex items-center justify-center text-surface-300 text-xs">IMG</div>}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium truncate">{item.productName}</p>
                        <p className="text-xs text-surface-400 font-mono">{item.productSKU}</p>
                      </div>
                      <div className="text-right text-sm">
                        <p className="font-semibold">₦{item.lineTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
                        <p className="text-xs text-surface-400">₦{item.unitPrice.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} × {item.quantity}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </>
          )}

          {/* ── History tab ── */}
          {activeTab === 'history' && (
            <div>
              <div className="flex items-center justify-between mb-5">
                <div>
                  <h3 className="font-semibold text-surface-900">Status Change History</h3>
                  <p className="text-xs text-surface-400 mt-0.5">
                    Full audit trail — every status change recorded with who made it and when
                  </p>
                </div>
                <button onClick={loadHistory} className="btn-secondary btn-sm gap-1.5 text-xs">
                  <Clock className="w-3.5 h-3.5" /> Refresh
                </button>
              </div>
              <StatusTimeline logs={logs} loading={logsLoading} />
            </div>
          )}
        </div>
      ) : null}
    </Modal>
  )
}

// ── Main Orders Page ──────────────────────────────────────────────────────────
export default function AdminOrders() {
  const [orders, setOrders]           = useState([])
  const [total, setTotal]             = useState(0)
  const [loading, setLoading]         = useState(true)
  const [page, setPage]               = useState(1)
  const [statusFilter, setStatusFilter] = useState('')
  const [selectedOrderId, setSelectedOrderId] = useState(null)
  const [modalOpen, setModalOpen]     = useState(false)

  const pageSize   = 20
  const totalPages = Math.ceil(total / pageSize)

  const load = () => {
    setLoading(true)
    orderApi.getAll({ page, pageSize, status: statusFilter || undefined })
      .then(r => { setOrders(r.data.items); setTotal(r.data.total) })
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [page, statusFilter])

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Orders</h1>
            <p className="text-surface-400 text-sm mt-0.5">{total} total orders</p>
          </div>
          <select className="input w-auto text-sm"
            value={statusFilter} onChange={e => { setStatusFilter(e.target.value); setPage(1) }}>
            <option value="">All Statuses</option>
            {ORDER_STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>

        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Order #</th>
                <th>Customer</th>
                <th>Date</th>
                <th>Items</th>
                <th>Total</th>
                <th>Status</th>
                <th>Payment</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={8} className="text-center py-12"><Spinner /></td></tr>
              ) : orders.length === 0 ? (
                <tr><td colSpan={8} className="text-center py-12 text-surface-400">No orders found</td></tr>
              ) : orders.map(o => (
                <tr key={o.id}>
                  <td><span className="font-mono text-xs text-brand-600 font-medium">{o.orderNumber}</span></td>
                  <td><span className="text-sm font-medium">{o.customerName}</span></td>
                  <td><span className="text-sm text-surface-500">{format(new Date(o.createdAt), 'MMM d, yyyy')}</span></td>
                  <td><span className="text-sm text-surface-500">{o.itemCount} items</span></td>
                  <td><span className="font-semibold">₦{o.total.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></td>
                  <td><StatusBadge status={o.status} /></td>
                  <td><StatusBadge status={o.paymentStatus} /></td>
                  <td>
                    <button onClick={() => { setSelectedOrderId(o.id); setModalOpen(true) }}
                      className="btn-icon">
                      <Eye className="w-3.5 h-3.5" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <Pagination page={page} totalPages={totalPages} onChange={setPage} />

        <OrderDetailModal
          open={modalOpen}
          onClose={() => setModalOpen(false)}
          orderId={selectedOrderId}
          onUpdated={load}
        />
      </div>
    </AdminLayout>
  )
}