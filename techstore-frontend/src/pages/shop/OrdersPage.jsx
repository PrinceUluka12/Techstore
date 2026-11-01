import { useEffect, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Package, ArrowLeft, ChevronRight, Clock, CheckCircle, Truck, XCircle, RefreshCw, Store } from 'lucide-react'
import { orderApi } from '../../services/api'
import { Navbar, Footer } from '../../components/layout/ShopLayout'
import { BankAccountList, useBankAccounts } from '../../components/shop/BankAccounts'
import { StatusBadge, STATUS_LABELS, LoadingPage, EmptyState, Pagination, ProductImage, PageTitle, Input, Spinner } from '../../components/ui'
import { emailRules, requiredText, showApiError } from '../../utils/validation'
import { format } from 'date-fns'

const STATUS_ICONS = {
  Pending:    Clock,
  Confirmed:  CheckCircle,
  Processing: RefreshCw,
  Shipped:    Truck,
  ReadyForPickup: Store,
  Delivered:  CheckCircle,
  Cancelled:  XCircle,
  Refunded:   RefreshCw,
}

// What the customer sees for each status
const customerLabel = (status, order) =>
  status === 'Pending' ? (order?.paymentStatus === 'Pending' ? 'Awaiting payment' : 'Pending')
  : status === 'Confirmed' ? 'Payment received'
  : status === 'Processing' ? 'Being prepared'
  : status === 'Shipped' ? 'On its way'
  : status === 'Delivered' && order?.fulfilment === 'Pickup' ? 'Collected'
  : STATUS_LABELS[status] ?? status

const naira = v => `₦${Number(v).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`

const STATUS_COLORS = {
  Pending:    'text-amber-500 bg-amber-50',
  Confirmed:  'text-blue-500 bg-blue-50',
  Processing: 'text-purple-500 bg-purple-50',
  Shipped:    'text-cyan-500 bg-cyan-50',
  ReadyForPickup: 'text-cyan-500 bg-cyan-50',
  Delivered:  'text-emerald-500 bg-emerald-50',
  Cancelled:  'text-red-500 bg-red-50',
  Refunded:   'text-surface-400 bg-surface-50',
}

// guestToken: an order placed without an account, read through its private link
function OrderTimeline({ orderId, guestToken, fulfilment }) {
  const [logs, setLogs] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    (guestToken ? orderApi.guestHistory(guestToken) : orderApi.getHistory(orderId))
      .then(r => setLogs(r.data))
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [orderId, guestToken])

  if (loading) return null
  if (!logs.length) return null

  return (
    <div className="card p-5">
      <h2 className="font-semibold mb-4">Order Timeline</h2>
      <div className="space-y-0">
        {logs.map((log, i) => {
          const Icon = STATUS_ICONS[log.toStatus] ?? Clock
          const colorClass = STATUS_COLORS[log.toStatus] ?? 'text-surface-400 bg-surface-50'
          return (
            <div key={log.id} className="flex gap-3 pb-4 last:pb-0 relative">
              {i < logs.length - 1 && (
                <div className="absolute left-4 top-8 bottom-0 w-px bg-surface-100" />
              )}
              <div className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 z-10 ${colorClass}`}>
                <Icon className="w-4 h-4" />
              </div>
              <div className="flex-1 pt-0.5">
                <p className="text-sm font-medium text-surface-900">
                  {i === 0 && log.fromStatus === log.toStatus ? 'Order placed' : customerLabel(log.toStatus, { fulfilment })}
                </p>
                {log.note && <p className="text-xs text-surface-500 mt-0.5">{log.note}</p>}
                <p className="text-xs text-surface-400 mt-1">
                  {format(new Date(log.changedAt), 'MMM d, yyyy · h:mm a')}
                  {log.changedByName !== 'System' && ` · ${log.changedByName}`}
                </p>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

export function MyOrdersPage() {
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const pageSize = 10

  useEffect(() => {
    setLoading(true)
    orderApi.myOrders({ page, pageSize }).then(r => {
      // { items, totalPages }; the API sent a bare list before paging was added
      const data = Array.isArray(r.data) ? { items: r.data, totalPages: 1 } : r.data
      setOrders(data.items)
      setTotalPages(data.totalPages)
    }).finally(() => setLoading(false))
  }, [page])

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>My Orders</PageTitle>
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
                  <StatusBadge status={order.status} label={customerLabel(order.status, order)} />
                  <span className="font-semibold text-surface-900 whitespace-nowrap">₦{order.total.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                  <ChevronRight className="w-4 h-4 text-surface-300 group-hover:text-surface-600 transition-colors flex-shrink-0" />
                </div>
              </Link>
            ))}
            <Pagination page={page} totalPages={totalPages} onChange={p => { setPage(p); window.scrollTo(0, 0) }} />
          </div>
        )}
      </main>
      <Footer />
    </div>
  )
}

// Where to send the money for an unpaid bank-transfer order
function PayByTransfer({ order }) {
  const accounts = useBankAccounts()
  if (!accounts?.length) return null
  return (
    <div className="card p-5 border-amber-200 bg-amber-50/40">
      <h2 className="font-semibold mb-1">Pay by bank transfer</h2>
      <p className="text-sm text-surface-600 mb-3">
        Transfer <strong>{naira(order.total)}</strong> to {accounts.length > 1 ? 'one of these accounts' : 'this account'}.
        Use <span className="font-mono font-semibold">{order.orderNumber}</span> or your name as the reference.
      </p>
      {order.paymentDueAt && (
        <p className="text-sm font-medium text-amber-800 bg-amber-100/60 rounded-lg px-3 py-2 mb-3">
          Please pay by {format(new Date(order.paymentDueAt), "h:mm a 'on' MMM d")}, or the order will be cancelled.
        </p>
      )}
      <BankAccountList accounts={accounts} />
      <p className="text-xs text-surface-400 mt-3">We'll confirm your order once the payment arrives, usually within 1–2 business hours.</p>
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
    <OrderView order={order} top={
      <Link to="/account/orders" className="inline-flex items-center gap-2 text-sm text-surface-500 hover:text-surface-900 mb-6">
        <ArrowLeft className="w-4 h-4" /> Back to orders
      </Link>
    } />
  )
}

// An order placed without an account, opened from its private link (/orders/track/:token)
export function GuestOrderPage() {
  const { token } = useParams()
  const [order, setOrder] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    orderApi.guestOrder(token).then(r => setOrder(r.data)).catch(() => setOrder(null)).finally(() => setLoading(false))
  }, [token])

  if (loading) return <><Navbar /><LoadingPage /></>
  if (!order) return (
    <>
      <Navbar />
      <div className="page-container py-16 text-center">
        <p className="text-surface-500 mb-4">We couldn't find that order.</p>
        <Link to="/track-order" className="btn-primary">Find your order</Link>
      </div>
    </>
  )

  return (
    <OrderView order={order} guestToken={token} top={
      <p className="card p-4 mb-6 bg-brand-50/40 text-sm text-surface-700">
        <strong>Keep this page's link:</strong> it's how you check this order without an account. You can
        also find it with <Link to="/track-order" className="text-brand-600 hover:underline">Track an order</Link>,
        using your order number and email.
      </p>
    } />
  )
}

// Ordered without an account: the order number and the email used find the order's link
export function TrackOrderPage() {
  const { register, handleSubmit, setError, formState: { errors } } = useForm({ mode: 'onTouched' })
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      const { data: found } = await orderApi.findGuestOrder({ orderNumber: data.orderNumber.trim(), email: data.email.trim() })
      navigate(`/orders/track/${found.token}`)
    } catch (err) {
      showApiError(err, setError, "We couldn't find that order")
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>Track an Order</PageTitle>
      <main className="flex-1 page-container py-10">
        <div className="max-w-md">
          <h1 className="font-display text-2xl font-semibold">Track an order</h1>
          <p className="text-surface-500 text-sm mt-1">Ordered without an account? Enter your order number and the email you used.</p>
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="card p-6 mt-6 space-y-4">
            <Input label="Order number" placeholder="TS-20261101-A1B2C3" error={errors.orderNumber?.message}
              {...register('orderNumber', requiredText('Order number', 40))} />
            <Input label="Email" type="email" autoComplete="email" placeholder="you@example.com" error={errors.email?.message}
              {...register('email', emailRules)} />
            <button type="submit" disabled={loading} className="btn-primary w-full">
              {loading ? <Spinner size="sm" /> : 'Find my order'}
            </button>
          </form>
          <p className="text-sm text-surface-500 mt-4">
            Have an account?{' '}
            <Link to="/login" state={{ from: '/account/orders' }} className="text-brand-600 hover:underline">Sign in</Link>{' '}
            to see all your orders.
          </p>
        </div>
      </main>
      <Footer />
    </div>
  )
}

// The order itself: items, timeline, how to pay, totals and where it's going
function OrderView({ order, guestToken, top }) {
  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>{`Order ${order.orderNumber}`}</PageTitle>
      <main className="flex-1 page-container py-10">
        {top}

        <div className="flex flex-wrap items-start justify-between gap-3 mb-6">
          <div>
            <h1 className="font-mono text-2xl font-semibold">{order.orderNumber}</h1>
            <p className="text-surface-400 text-sm mt-1">{format(new Date(order.createdAt), 'MMMM d, yyyy · h:mm a')}</p>
          </div>
          <StatusBadge status={order.status} label={customerLabel(order.status, order)} />
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
                        ? <ProductImage src={item.productImageUrl} alt={item.productName} className="w-full h-full object-contain p-1" />
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
            <OrderTimeline orderId={order.id} guestToken={guestToken} fulfilment={order.fulfilment} />
          </div>

          <div className="space-y-4">
            {order.paymentMethod === 'BankTransfer' && order.paymentStatus === 'Pending'
              && !['Cancelled', 'Refunded'].includes(order.status) && <PayByTransfer order={order} />}
            <div className="card p-5">
              <h2 className="font-semibold mb-3">Order Summary</h2>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between text-surface-500"><span>Subtotal</span><span>₦{order.subTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></div>
                {order.discountAmount > 0 && (
                  <div className="flex justify-between text-emerald-600">
                    <span>Discount{order.couponCode ? ` (${order.couponCode})` : ''}</span>
                    <span>–₦{order.discountAmount.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                  </div>
                )}
                {order.tax > 0 && ( // only orders from before VAT was dropped
                  <div className="flex justify-between text-surface-500"><span>VAT (7.5%)</span><span>₦{order.tax.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></div>
                )}
                <div className="flex justify-between text-surface-500">
                  <span>{order.fulfilment === 'Pickup' ? 'Pickup' : 'Delivery'}</span>
                  <span>{order.shippingCost === 0 ? 'Free' : naira(order.shippingCost)}</span>
                </div>
                <div className="flex justify-between font-semibold pt-2 border-t border-surface-100 text-base">
                  <span>Total</span><span>₦{order.total.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
              </div>
            </div>
            {order.fulfilment === 'Pickup' ? (
              <div className="card p-5">
                <h2 className="font-semibold mb-3">Collect From</h2>
                <address className="text-sm text-surface-600 not-italic leading-relaxed">
                  <span className="font-medium text-surface-900">{order.storeName}</span><br />
                  {order.pickupAddress && <>{order.pickupAddress}<br /></>}
                  {order.pickupPhone}
                </address>
                <p className="text-xs text-surface-400 mt-3">We'll email you when it's ready. Bring your order number.</p>
              </div>
            ) : (
              <div className="card p-5">
                <h2 className="font-semibold mb-3">Delivering To</h2>
                <address className="text-sm text-surface-600 not-italic leading-relaxed">
                  {order.shippingAddress}<br />
                  {order.shippingCity}, {order.shippingProvince}<br />
                  {order.shippingPostalCode}<br />
                  {order.shippingCountry}
                  {order.shippingPhone && <><br />{order.shippingPhone}</>}
                </address>
              </div>
            )}
          </div>
        </div>
      </main>
      <Footer />
    </div>
  )
}