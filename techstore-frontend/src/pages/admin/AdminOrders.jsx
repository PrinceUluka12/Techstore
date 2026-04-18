import { useEffect, useState } from 'react'
import { Search, Eye, ChevronDown } from 'lucide-react'
import { orderApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, StatusBadge, Spinner, Pagination } from '../../components/ui'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const ORDER_STATUSES = ['Pending','Confirmed','Processing','Shipped','Delivered','Cancelled','Refunded']

function OrderDetailModal({ open, onClose, orderId, onUpdated }) {
  const [order, setOrder] = useState(null)
  const [loading, setLoading] = useState(false)
  const [updating, setUpdating] = useState(false)

  useEffect(() => {
    if (!orderId || !open) return
    setLoading(true)
    orderApi.getById(orderId).then(r => setOrder(r.data)).finally(() => setLoading(false))
  }, [orderId, open])

  const updateStatus = async (status) => {
    setUpdating(true)
    try {
      await orderApi.updateStatus(orderId, { status })
      toast.success(`Order status → ${status}`)
      const { data } = await orderApi.getById(orderId)
      setOrder(data)
      onUpdated()
    } catch { toast.error('Failed to update status') }
    finally { setUpdating(false) }
  }

  return (
    <Modal open={open} onClose={onClose} title={order ? `Order ${order.orderNumber}` : 'Order Details'} size="xl">
      {loading ? (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      ) : order ? (
        <div className="space-y-6">
          {/* Status + actions */}
          <div className="flex flex-wrap items-center gap-3">
            <StatusBadge status={order.status} />
            <StatusBadge status={order.paymentStatus} />
            <span className="text-sm text-surface-400">{format(new Date(order.createdAt), 'MMM d, yyyy · h:mm a')}</span>
            <div className="ml-auto flex gap-2 flex-wrap">
              {ORDER_STATUSES.filter(s => s !== order.status).slice(0, 3).map(s => (
                <button key={s} onClick={() => updateStatus(s)} disabled={updating}
                  className="btn-secondary btn-sm text-xs">
                  → {s}
                </button>
              ))}
            </div>
          </div>

          <div className="grid sm:grid-cols-2 gap-6">
            {/* Customer + shipping */}
            <div>
              <h3 className="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-2">Customer</h3>
              <p className="font-medium text-surface-900">{order.customerName}</p>
              <p className="text-sm text-surface-500">{order.customerEmail}</p>

              <h3 className="text-xs font-semibold text-surface-400 uppercase tracking-wider mt-4 mb-2">Shipping Address</h3>
              <address className="text-sm text-surface-600 not-italic leading-relaxed">
                {order.shippingAddress}<br />
                {order.shippingCity}, {order.shippingProvince} {order.shippingPostalCode}<br />
                {order.shippingCountry}
              </address>
            </div>

            {/* Totals */}
            <div>
              <h3 className="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-2">Order Total</h3>
              <div className="space-y-1.5 text-sm">
                <div className="flex justify-between text-surface-500"><span>Subtotal</span><span>${order.subTotal.toFixed(2)}</span></div>
                <div className="flex justify-between text-surface-500"><span>Tax (HST)</span><span>${order.tax.toFixed(2)}</span></div>
                <div className="flex justify-between text-surface-500"><span>Shipping</span><span>${order.shippingCost.toFixed(2)}</span></div>
                <div className="flex justify-between font-bold text-surface-900 border-t border-surface-100 pt-2 mt-2">
                  <span>Total</span><span>${order.total.toFixed(2)}</span>
                </div>
              </div>
            </div>
          </div>

          {/* Items */}
          <div>
            <h3 className="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-3">Items ({order.items.length})</h3>
            <div className="rounded-xl border border-surface-100 overflow-hidden">
              {order.items.map((item, i) => (
                <div key={item.id} className={`flex items-center gap-3 p-3 ${i > 0 ? 'border-t border-surface-100' : ''}`}>
                  <div className="w-10 h-10 rounded-lg bg-surface-100 flex-shrink-0 overflow-hidden">
                    {item.productImageUrl
                      ? <img src={item.productImageUrl} alt={item.productName} className="w-full h-full object-cover" />
                      : <div className="w-full h-full flex items-center justify-center text-surface-300 text-xs">IMG</div>}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">{item.productName}</p>
                    <p className="text-xs text-surface-400 font-mono">{item.productSKU}</p>
                  </div>
                  <div className="text-right text-sm">
                    <p className="font-semibold">${item.lineTotal.toFixed(2)}</p>
                    <p className="text-xs text-surface-400">${item.unitPrice.toFixed(2)} × {item.quantity}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : null}
    </Modal>
  )
}

export default function AdminOrders() {
  const [orders, setOrders] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState('')
  const [selectedOrderId, setSelectedOrderId] = useState(null)
  const [modalOpen, setModalOpen] = useState(false)

  const pageSize = 20
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
        <div className="flex items-center justify-between">
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
                  <td><span className="font-semibold">${o.total.toFixed(2)}</span></td>
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
