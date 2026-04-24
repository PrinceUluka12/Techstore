import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { Package, ArrowLeft, ChevronRight } from 'lucide-react'
import { orderApi } from '../../services/api'
import { Navbar, Footer } from '../../components/layout/ShopLayout'
import { StatusBadge, LoadingPage, EmptyState, Pagination } from '../../components/ui'
import { format } from 'date-fns'

export function MyOrdersPage() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const pageSize = 10

  useEffect(() => {
    setLoading(true)
    orderApi.myOrders({ page, pageSize }).then(r => {
      setOrders(r.data)
      setTotal(r.data.length) // simplified - API returns array for "my orders"
    }).finally(() => setLoading(false))
  }, [page])

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-1 page-container py-10">
        <h1 className="font-display text-2xl font-semibold mb-6">My Orders</h1>
        {loading ? <LoadingPage /> : orders.length === 0 ? (
          <EmptyState icon={Package} title="No orders yet"
            description="Once you place an order, it will appear here."
            action={<Link to="/products" className="btn-primary">Start Shopping</Link>} />
        ) : (
          <div className="space-y-3">
            {orders.map(order => (
              <Link key={order.id} to={`/account/orders/${order.id}`}
                className="card-hover p-5 flex items-center gap-4 group">
                <div className="w-10 h-10 rounded-xl bg-brand-50 flex items-center justify-center flex-shrink-0">
                  <Package className="w-5 h-5 text-brand-600" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-mono text-sm font-medium text-surface-900">{order.orderNumber}</p>
                  <p className="text-xs text-surface-400 mt-0.5">
                    {format(new Date(order.createdAt), 'MMM d, yyyy')} · {order.itemCount} items
                  </p>
                </div>
                <div className="flex items-center gap-2 sm:gap-4">
                  <StatusBadge status={order.status} />
                  <span className="font-semibold text-surface-900 whitespace-nowrap">₦{order.total.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                  <ChevronRight className="w-4 h-4 text-surface-300 group-hover:text-surface-600 transition-colors flex-shrink-0" />
                </div>
              </Link>
            ))}
          </div>
        )}
      </main>
      <Footer />
    </div>
  )
}

export function OrderDetailPage() {
  const { id } = useParams()
  const [order, setOrder] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    orderApi.getById(id).then(r => setOrder(r.data)).finally(() => setLoading(false))
  }, [id])

  if (loading) return <><Navbar /><LoadingPage /></>
  if (!order) return <><Navbar /><div className="page-container py-16 text-center text-surface-400">Order not found</div></>

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-1 page-container py-10">
        <Link to="/account/orders" className="inline-flex items-center gap-2 text-sm text-surface-500 hover:text-surface-900 mb-6">
          <ArrowLeft className="w-4 h-4" /> Back to orders
        </Link>

        <div className="flex flex-wrap items-start justify-between gap-3 mb-6">
          <div>
            <h1 className="font-display text-2xl font-semibold font-mono">{order.orderNumber}</h1>
            <p className="text-surface-400 text-sm mt-1">{format(new Date(order.createdAt), 'MMMM d, yyyy · h:mm a')}</p>
          </div>
          <StatusBadge status={order.status} />
        </div>

        <div className="grid lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-4">
            <div className="card p-6">
              <h2 className="font-semibold mb-4">Items</h2>
              <div className="space-y-4">
                {order.items.map(item => (
                  <div key={item.id} className="flex gap-3">
                    <div className="w-14 h-14 bg-white border border-surface-100 rounded-xl flex-shrink-0 overflow-hidden">
                      {item.productImageUrl
                        ? <img src={item.productImageUrl} alt={item.productName} className="w-full h-full object-contain p-1" />
                        : <div className="w-full h-full flex items-center justify-center text-surface-200"><Package className="w-5 h-5" /></div>}
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium">{item.productName}</p>
                      <p className="text-xs text-surface-400">{item.productSKU}</p>
                      <p className="text-xs text-surface-500 mt-1">₦{item.unitPrice.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} × {item.quantity}</p>
                    </div>
                    <p className="font-semibold text-sm">₦{item.lineTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="space-y-4">
            <div className="card p-5">
              <h2 className="font-semibold mb-3">Order Summary</h2>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between text-surface-500"><span>Subtotal</span><span>₦{order.subTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></div>
                <div className="flex justify-between text-surface-500"><span>Tax</span><span>₦{order.tax.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></div>
                <div className="flex justify-between text-surface-500"><span>Shipping</span><span>₦{order.shippingCost.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></div>
                <div className="flex justify-between font-semibold pt-2 border-t border-surface-100 text-base">
                  <span>Total</span><span>₦{order.total.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
              </div>
            </div>
            <div className="card p-5">
              <h2 className="font-semibold mb-3">Shipping To</h2>
              <address className="text-sm text-surface-600 not-italic leading-relaxed">
                {order.shippingAddress}<br />
                {order.shippingCity}, {order.shippingProvince}<br />
                {order.shippingPostalCode}<br />
                {order.shippingCountry}
              </address>
            </div>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  )
}