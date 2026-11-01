import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  Package, Building2, Lock,
  ChevronDown, ChevronUp, CheckCircle, Truck, Tag, X, Store
} from 'lucide-react'
import toast from 'react-hot-toast'
import { authApi, orderApi, deliveryApi } from '../../services/api'
import { useAuthStore, useCartStore } from '../../store'
import { Input, Spinner, ProductImage, LoadingPage, PageTitle } from '../../components/ui'
import { Navbar, Footer } from '../../components/layout/ShopLayout'
import { BankAccountList, useBankAccounts } from '../../components/shop/BankAccounts'
import {
  emailRules, nameRules, passwordRules, phoneRules, postalCodeRules,
  requiredText, maxText, normalizePhone, showApiError,
} from '../../utils/validation'
import { finishSignIn } from '../../utils/signIn'
import { INFO_PAGES } from './infoLinks'

const InfoLink = ({ page }) => (
  <Link to={INFO_PAGES[page].path} target="_blank" className="text-brand-600 hover:underline">{INFO_PAGES[page].label}</Link>
)

const NIGERIAN_STATES = [
  'Abia','Adamawa','Akwa Ibom','Anambra','Bauchi','Bayelsa','Benue','Borno',
  'Cross River','Delta','Ebonyi','Edo','Ekiti','Enugu','FCT - Abuja','Gombe',
  'Imo','Jigawa','Kaduna','Kano','Katsina','Kebbi','Kogi','Kwara','Lagos',
  'Nasarawa','Niger','Ogun','Ondo','Osun','Oyo','Plateau','Rivers','Sokoto',
  'Taraba','Yobe','Zamfara',
]

// ── Login ─────────────────────────────────────────────────────────────────────
export function LoginPage() {
  const { register, handleSubmit, setError, formState: { errors } } = useForm({ mode: 'onTouched' })
  const [loading, setLoading] = useState(false)
  const { login } = useAuthStore()
  const { mergeGuestCart } = useCartStore()
  const navigate = useNavigate()
  const { state } = useLocation() // where they came from and what they were doing (utils/signIn)

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      const { data: auth } = await authApi.login({ email: data.email.trim(), password: data.password })
      login(auth)
      await mergeGuestCart() // a cart filled before signing in joins the account's
      toast.success(`Welcome back, ${auth.firstName}!`)
      await finishSignIn(state, navigate, auth.role === 'Admin' ? '/admin' : '/')
    } catch (err) {
      showApiError(err, setError, 'Login failed')
    } finally { setLoading(false) }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>Sign In</PageTitle>
      <div className="flex-1 flex items-center justify-center p-4">
        <div className="w-full max-w-sm animate-fade-up">
          <div className="text-center mb-8">
            <div className="w-16 h-16 flex items-center justify-center mx-auto mb-4">
              <img src="/hytel-logo.png" alt="Hytel Phones" className="w-full h-full object-contain" />
            </div>
            <h1 className="font-display text-2xl font-semibold">Welcome back</h1>
            <p className="text-surface-500 text-sm mt-1">{state?.message ?? 'Sign in to your account'}</p>
          </div>
          <div className="card p-6">
            <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
              <Input label="Email" type="email" placeholder="you@example.com" autoComplete="email"
                error={errors.email?.message}
                {...register('email', emailRules)} />
              {/* No strength rules here: older accounts may predate them */}
              <Input label="Password" type="password" placeholder="••••••••" autoComplete="current-password"
                error={errors.password?.message}
                {...register('password', { required: 'Password is required' })} />
              <div className="flex justify-end">
                <Link to="/forgot-password" className="text-xs text-brand-600 hover:underline">Forgot password?</Link>
              </div>
              <button type="submit" disabled={loading} className="btn-primary w-full btn-lg">
                {loading ? <Spinner size="sm" /> : 'Sign In'}
              </button>
            </form>
          </div>
          <p className="text-center text-sm text-surface-500 mt-4">
            Don't have an account?{' '}
            <Link to="/register" state={state} className="text-brand-600 font-medium hover:underline">Create one</Link>
          </p>
        </div>
      </div>
    </div>
  )
}

// ── Register ──────────────────────────────────────────────────────────────────
export function RegisterPage() {
  // Passed on from the sign-in page (utils/signIn), or from a guest's order page with their email
  const { state } = useLocation()
  const { register, handleSubmit, getValues, setError, formState: { errors } } =
    useForm({ mode: 'onTouched', defaultValues: { email: state?.email ?? '' } })
  const [loading, setLoading] = useState(false)
  const { login } = useAuthStore()
  const navigate = useNavigate()

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      const { data: auth } = await authApi.register({
        firstName: data.firstName.trim(),
        lastName: data.lastName.trim(),
        email: data.email.trim(),
        password: data.password,
        // The API rejects an empty string, so a blank phone must be sent as null
        phone: normalizePhone(data.phone) || null,
      })
      login(auth)
      await useCartStore.getState().mergeGuestCart()
      toast.success(`Welcome, ${auth.firstName}!`)
      await finishSignIn(state, navigate, '/')
    } catch (err) {
      showApiError(err, setError, 'Registration failed')
    } finally { setLoading(false) }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>Create Account</PageTitle>
      <div className="flex-1 flex items-center justify-center p-4 py-12">
        <div className="w-full max-w-sm animate-fade-up">
          <div className="text-center mb-8">
            <h1 className="font-display text-2xl font-semibold">Create account</h1>
            <p className="text-surface-500 text-sm mt-1">{state?.message ?? 'Join Hytel Phones today'}</p>
          </div>
          <div className="card p-6">
            <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
              <div className="grid grid-cols-1 xs:grid-cols-2 gap-3">
                <Input label="First Name" placeholder="Jane" autoComplete="given-name"
                  error={errors.firstName?.message}
                  {...register('firstName', nameRules('First name'))} />
                <Input label="Last Name" placeholder="Doe" autoComplete="family-name"
                  error={errors.lastName?.message}
                  {...register('lastName', nameRules('Last name'))} />
              </div>
              <Input label="Email" type="email" placeholder="you@example.com" autoComplete="email"
                error={errors.email?.message}
                {...register('email', emailRules)} />
              <Input label="Phone (optional)" type="tel" placeholder="08012345678" autoComplete="tel"
                error={errors.phone?.message}
                {...register('phone', phoneRules)} />
              <Input label="Password" type="password" placeholder="8+ characters, 1 uppercase, 1 number"
                autoComplete="new-password"
                error={errors.password?.message}
                {...register('password', passwordRules)} />
              <Input label="Confirm Password" type="password" placeholder="Repeat password"
                autoComplete="new-password"
                error={errors.confirmPassword?.message}
                {...register('confirmPassword', {
                  required: 'Please confirm your password',
                  validate: v => v === getValues('password') || 'Passwords do not match'
                })} />
              <button type="submit" disabled={loading} className="btn-primary w-full btn-lg">
                {loading ? <Spinner size="sm" /> : 'Create Account'}
              </button>
              <p className="text-center text-xs text-surface-400">
                By creating an account you agree to our <InfoLink page="terms" /> and <InfoLink page="privacy" />.
              </p>
            </form>
          </div>
          <p className="text-center text-sm text-surface-500 mt-4">
            Already have an account?{' '}
            <Link to="/login" state={state} className="text-brand-600 font-medium hover:underline">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  )
}

// ── Bank transfer details ─────────────────────────────────────────────────────
// accounts: from useBankAccounts (null while loading); refField: the sender-name input's props and error
function BankTransferDetails({ orderTotal, accounts, refField }) {
  if (accounts === null)
    return <div className="mt-4 pt-4 border-t border-surface-100 flex justify-center py-6"><Spinner /></div>

  if (accounts.length === 0) return (
    <div className="mt-4 pt-4 border-t border-surface-100">
      <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 text-sm text-amber-800">
        <p className="font-semibold mb-1">Online orders are paused</p>
        <p className="text-xs text-amber-700">We can't take payment online right now. Please call or visit one of our shops to buy.</p>
      </div>
    </div>
  )

  return (
    <div className="space-y-4 mt-4 pt-4 border-t border-surface-100">
      <div className="bg-amber-50 border border-amber-200 rounded-xl p-4">
        <p className="text-sm font-semibold text-amber-800 mb-1">How it works</p>
        <ol className="text-xs text-amber-700 space-y-1 list-decimal list-inside">
          <li>Transfer <strong>{naira(orderTotal)}</strong> to {accounts.length > 1 ? 'one of the accounts' : 'the account'} below <strong>within 24 hours</strong>, or the order is cancelled</li>
          <li>Use your order number or your name as the payment reference</li>
          <li>We'll email you as soon as the payment is confirmed</li>
        </ol>
      </div>

      <BankAccountList accounts={accounts} />

      <Input
        label="Name on the paying account (optional)"
        placeholder="e.g. John Doe — helps us match your payment"
        error={refField.error}
        {...refField.props}
      />

      <div className="flex items-center gap-2 text-xs text-surface-400 bg-surface-50 rounded-xl px-3 py-2.5">
        <Building2 className="w-3.5 h-3.5 text-brand-500 flex-shrink-0" />
        Orders are processed after payment is confirmed by our team.
      </div>
    </div>
  )
}

const naira = v => `₦${Number(v).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`

// ── Coupon Input ──────────────────────────────────────────────────────────────
function CouponInput({ onApply, onRemove, appliedCode, discount, loading }) {
  const [code, setCode] = useState('')

  const handleApply = () => {
    if (code.trim()) onApply(code.trim().toUpperCase())
  }

  if (appliedCode) {
    return (
      <div className="flex items-center justify-between bg-emerald-50 border border-emerald-200 rounded-xl px-3 py-2.5">
        <div className="flex items-center gap-2">
          <Tag className="w-3.5 h-3.5 text-emerald-600 flex-shrink-0" />
          <span className="text-sm font-semibold text-emerald-700 font-mono">{appliedCode}</span>
          <span className="text-xs text-emerald-600">–₦{discount.toLocaleString('en-NG', { minimumFractionDigits: 2 })}</span>
        </div>
        <button type="button" onClick={onRemove} className="text-emerald-500 hover:text-emerald-700">
          <X className="w-4 h-4" />
        </button>
      </div>
    )
  }

  return (
    <div className="flex gap-2">
      <input
        type="text"
        className="input flex-1 font-mono uppercase text-sm"
        placeholder="Coupon code"
        value={code}
        onChange={e => setCode(e.target.value.toUpperCase())}
        onKeyDown={e => e.key === 'Enter' && (e.preventDefault(), handleApply())}
      />
      <button type="button" onClick={handleApply} disabled={loading || !code.trim()}
        className="btn-secondary px-4 text-sm whitespace-nowrap">
        {loading ? <Spinner size="sm" /> : 'Apply'}
      </button>
    </div>
  )
}

// ── Checkout ──────────────────────────────────────────────────────────────────
// API field names → checkout form field names, for showApiError
const CHECKOUT_FIELDS = {
  shippingfirstname: 'firstName', shippinglastname: 'lastName', shippingaddress: 'address',
  shippingcity: 'city', shippingprovince: 'province', shippingpostalcode: 'postalCode',
  shippingphone: 'phone', transactionid: 'transferRef',
}
// A guest checkout sends the same details under "order"
const GUEST_FIELDS = {
  email: 'email',
  ...Object.fromEntries(Object.entries(CHECKOUT_FIELDS).map(([key, field]) => [`order.${key}`, field])),
}

export function CheckoutPage() {
  const { register, handleSubmit, setError, watch, formState: { errors } } = useForm({ mode: 'onTouched' })
  const [loading, setLoading]               = useState(false)
  const [method, setMethod] = useState('Delivery') // or 'Pickup'
  const [rates, setRates]   = useState(null)        // states we deliver to: [{ state, fee }]
  const [shops, setShops]   = useState(null)        // shops to collect from: [{ id, name, ..., hasStock }]
  const [shopId, setShopId] = useState(null)
  const accounts = useBankAccounts()            // nothing to pay into = online orders paused
  const { items, total, loaded, loadCart, clearLocal, guestLines, applyCoupon, removeCoupon, couponCode, discountAmount, couponLoading } = useCartStore()
  const guest = !useAuthStore().isAuthenticated // checking out without an account
  const navigate = useNavigate()
  const placed = useRef(false) // the cart empties when the order goes through; don't treat that as "nothing to buy"

  // After a reload the cart has to be fetched before we know whether there's anything to check out
  useEffect(() => { if (!loaded) loadCart() }, [loaded])
  useEffect(() => {
    if (loaded && !items.length && !placed.current) navigate('/products', { replace: true })
  }, [loaded, items.length])

  useEffect(() => { deliveryApi.list().then(r => setRates(r.data)).catch(() => setRates([])) }, [])
  // Which shops have everything in the cart; asked again when the cart changes
  useEffect(() => {
    if (loaded && items.length)
      (guest ? orderApi.guestPickupShops(guestLines()) : orderApi.pickupShops())
        .then(r => setShops(r.data)).catch(() => setShops([]))
  }, [loaded, items.length, total, guest])

  // Delivery is priced per state (same as OrderService); pickup is free. No VAT is charged.
  const pickup     = method === 'Pickup'
  const state      = watch('province')
  const rate       = rates?.find(r => r.state === state)
  const shop       = shops?.find(s => s.id === shopId)
  const discount   = discountAmount || 0
  const shipping   = pickup ? 0 : rate?.fee ?? null // null until a state with delivery is chosen
  const orderTotal = +(total - discount + (shipping ?? 0)).toFixed(2)
  const noDelivery = rates?.length === 0
  const blocked    = pickup ? !shop?.hasStock : noDelivery || (!!state && !rate)

  const handleCoupon = async (code) => {
    const result = await applyCoupon(code)
    if (result.success) toast.success(`Coupon applied! –₦${result.discountAmount.toLocaleString('en-NG', { minimumFractionDigits: 2 })}`)
    else toast.error(result.error || 'Invalid coupon')
  }

  const buildOrderPayload = (data) => ({
    shippingFirstName:  data.firstName.trim(),
    shippingLastName:   data.lastName.trim(),
    shippingAddress:    pickup ? '' : data.address.trim(),
    shippingCity:       pickup ? '' : data.city.trim(),
    shippingProvince:   pickup ? '' : data.province,
    shippingPostalCode: pickup ? '' : data.postalCode.trim(),
    shippingCountry:    'Nigeria',
    shippingPhone:      normalizePhone(data.phone) || null,
    fulfilment:         method,
    pickupStoreId:      pickup ? shopId : null,
    paymentMethod:      'BankTransfer', // the only option until card payments (Paystack) are set up
    transactionId:      data.transferRef?.trim() || null,
    notes:              data.notes?.trim() || null,
    couponCode:         couponCode || null,
  })

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      const { data: order } = guest
        ? await orderApi.guestCheckout({ email: data.email.trim(), items: guestLines(), order: buildOrderPayload(data) })
        : await orderApi.checkout(buildOrderPayload(data))
      placed.current = true
      clearLocal()
      toast.success('Order placed! Please complete your bank transfer.')
      // A guest follows their order through its private link
      navigate(guest ? `/orders/track/${order.guestToken}` : `/account/orders/${order.id}`)
    } catch (err) {
      showApiError(err, setError, 'Checkout failed', guest ? GUEST_FIELDS : CHECKOUT_FIELDS)
      const message = err.response?.data?.message ?? ''
      if (guest && /already an account/.test(message)) setError('email', { type: 'server', message })
      setLoading(false)
    }
  }

  if (!loaded) return <><Navbar /><LoadingPage /></>
  if (!items.length) return null // on the way to /products

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>Checkout</PageTitle>
      <main className="flex-1 page-container py-10">

        {/* Steps indicator */}
        <div className="flex items-center gap-2 mb-8 text-xs text-surface-400">
          <span className="flex items-center gap-1.5 text-emerald-600 font-medium">
            <CheckCircle className="w-3.5 h-3.5" /> Cart
          </span>
          <div className="flex-1 h-px bg-surface-200" />
          <span className="flex items-center gap-1.5 text-brand-600 font-semibold">
            <span className="w-5 h-5 rounded-full bg-brand-500 text-white flex items-center justify-center font-bold text-[10px]">2</span>
            Checkout
          </span>
          <div className="flex-1 h-px bg-surface-200" />
          <span className="flex items-center gap-1.5 text-surface-400">
            <span className="w-5 h-5 rounded-full border border-surface-300 flex items-center justify-center font-bold text-[10px]">3</span>
            Confirmation
          </span>
        </div>

        <div className="grid lg:grid-cols-3 gap-8">

          {/* ── Left — form ── */}
          <form onSubmit={handleSubmit(onSubmit)} noValidate className="lg:col-span-2 space-y-5">

            {/* Without an account: an email for the order's updates and its link */}
            {guest && (
              <div className="card p-6">
                <div className="flex flex-wrap items-baseline justify-between gap-2 mb-4">
                  <h2 className="font-semibold text-surface-900">Your email</h2>
                  <p className="text-sm text-surface-500">
                    Have an account?{' '}
                    <Link to="/login" state={{ from: '/checkout' }} className="text-brand-600 font-medium hover:underline">Sign in</Link>
                  </p>
                </div>
                <Input label="Email" type="email" autoComplete="email" placeholder="you@example.com"
                  error={errors.email?.message} {...register('email', emailRules)} />
                <p className="text-xs text-surface-400 mt-2">No account needed. We'll email your order details and a link to follow it.</p>
              </div>
            )}

            {/* Delivery or pickup */}
            <div className="card p-6">
              <div className="flex items-center gap-2 mb-5">
                <div className="w-7 h-7 rounded-lg bg-brand-50 flex items-center justify-center">
                  <Truck className="w-3.5 h-3.5 text-brand-600" />
                </div>
                <h2 className="font-semibold text-surface-900">Delivery or Pickup</h2>
              </div>

              <div className="grid grid-cols-2 gap-3 mb-5" role="radiogroup" aria-label="Delivery or pickup">
                {[['Delivery', Truck, 'To your address'], ['Pickup', Store, 'Free, from one of our shops']].map(([m, Icon, hint]) => (
                  <button key={m} type="button" role="radio" aria-checked={method === m} onClick={() => setMethod(m)}
                    className={`text-left rounded-xl border p-3 transition-colors
                      ${method === m ? 'border-brand-500 bg-brand-50 ring-1 ring-brand-500' : 'border-surface-200 hover:border-surface-300'}`}>
                    <span className="flex items-center gap-2 font-medium text-sm text-surface-900"><Icon className="w-4 h-4" /> {m}</span>
                    <span className="block text-xs text-surface-500 mt-0.5">{hint}</span>
                  </button>
                ))}
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <Input label="First Name" autoComplete="given-name" error={errors.firstName?.message}
                  {...register('firstName', nameRules('First name'))} />
                <Input label="Last Name" autoComplete="family-name" error={errors.lastName?.message}
                  {...register('lastName', nameRules('Last name'))} />
                <Input label="Phone Number" className="col-span-2" type="tel" autoComplete="tel"
                  placeholder="08012345678" error={errors.phone?.message}
                  {...register('phone', pickup || guest
                    ? { validate: v => normalizePhone(v) !== '' ? phoneRules.validate(v)
                        : pickup ? 'Enter a phone number so the shop can reach you' : 'Enter a phone number so we can reach you about your order' }
                    : phoneRules)} />

                {!pickup && (noDelivery ? (
                  <p className="col-span-2 text-sm bg-amber-50 border border-amber-200 text-amber-800 rounded-xl p-3">
                    We can't deliver yet. Choose <strong>Pickup</strong> to collect from one of our shops.
                  </p>
                ) : <>
                  <Input label="Street Address" className="col-span-2" autoComplete="street-address"
                    error={errors.address?.message}
                    placeholder="House number, street name"
                    {...register('address', requiredText('Street address', 200))} />
                  <Input label="City" autoComplete="address-level2" error={errors.city?.message}
                    {...register('city', requiredText('City', 100))} />
                  <div>
                    <label className="label">State {errors.province && <span className="text-red-500 text-xs ml-1">{errors.province.message}</span>}</label>
                    <select className="input w-full" {...register('province', { required: 'Select a state' })}>
                      <option value="">Select state…</option>
                      {NIGERIAN_STATES.map(s => {
                        const r = rates?.find(x => x.state === s)
                        return (
                          <option key={s} value={s} disabled={!!rates && !r}>
                            {s}{r ? ` — ${r.fee === 0 ? 'free delivery' : naira(r.fee)}` : rates ? ' (pickup only)' : ''}
                          </option>
                        )
                      })}
                    </select>
                  </div>
                  <Input label="Postal Code" placeholder="100001" inputMode="numeric" maxLength={6}
                    autoComplete="postal-code" error={errors.postalCode?.message}
                    {...register('postalCode', postalCodeRules)} />
                </>)}
              </div>

              {pickup && (
                <div className="mt-5 space-y-2">
                  <p className="label">Collect from</p>
                  {shops === null ? <div className="flex justify-center py-4"><Spinner /></div>
                  : shops.length === 0 ? <p className="text-sm text-surface-500">No shops are taking pickups right now.</p>
                  : shops.map(s => (
                    <label key={s.id}
                      className={`flex items-start gap-3 rounded-xl border p-3
                        ${s.hasStock ? 'cursor-pointer hover:border-surface-300' : 'opacity-60 cursor-not-allowed'}
                        ${shopId === s.id ? 'border-brand-500 bg-brand-50' : 'border-surface-200'}`}>
                      <input type="radio" name="pickupShop" className="mt-1" disabled={!s.hasStock}
                        checked={shopId === s.id} onChange={() => setShopId(s.id)} />
                      <span className="flex-1">
                        <span className="block font-medium text-sm text-surface-900">{s.name}</span>
                        {(s.address || s.city) && <span className="block text-xs text-surface-500">{[s.address, s.city].filter(Boolean).join(', ')}</span>}
                        {s.phone && <span className="block text-xs text-surface-500">{s.phone}</span>}
                        {!s.hasStock && <span className="block text-xs text-amber-700 mt-0.5">Doesn't have everything in your cart</span>}
                      </span>
                    </label>
                  ))}
                  <p className="text-xs text-surface-400">We'll email you when it's ready to collect. Bring your order number.</p>
                </div>
              )}
            </div>

            {/* Payment */}
            <div className="card p-6">
              <div className="flex items-center gap-2 mb-5">
                <div className="w-7 h-7 rounded-lg bg-brand-50 flex items-center justify-center">
                  <Building2 className="w-3.5 h-3.5 text-brand-600" />
                </div>
                <h2 className="font-semibold text-surface-900">Payment: Bank Transfer</h2>
              </div>

              <BankTransferDetails orderTotal={orderTotal} accounts={accounts}
                refField={{ props: register('transferRef', maxText(100)), error: errors.transferRef?.message }} />
            </div>

            {/* Notes */}
            <div className="card p-5">
              <label className="label">Order Notes <span className="text-surface-400 font-normal">(optional)</span></label>
              <textarea className="input min-h-[80px] resize-none"
                placeholder={pickup ? 'Anything the shop should know…' : 'Special delivery instructions…'}
                {...register('notes', maxText(500))} />
              {errors.notes && <p className="text-red-500 text-xs mt-1">{errors.notes.message}</p>}
            </div>

            {/* Place order */}
            <button type="submit" disabled={loading || !accounts?.length || blocked}
              className="btn-primary w-full btn-lg text-base font-bold gap-3">
              {loading
                ? <><Spinner size="sm" /> Processing…</>
                : <>
                    <Lock className="w-4 h-4" />
                    Place Order — Pay via Transfer
                  </>}
            </button>
            {blocked && accounts?.length > 0 && (
              <p className="text-center text-xs text-amber-700">
                {pickup ? 'Choose a shop that has everything in your cart.'
                  : noDelivery ? 'Choose Pickup to collect from one of our shops.'
                  : `We don't deliver to ${state} yet. Choose Pickup, or another state.`}
              </p>
            )}

            <p className="text-center text-xs text-surface-400 leading-relaxed">
              By placing this order you agree to our <InfoLink page="terms" />.
              We don't accept returns, so please check your order before you accept it
              (<InfoLink page="returns" />).
            </p>
          </form>

          {/* ── Right — order summary ── */}
          <div>
            <div className="card p-6 sticky top-24 space-y-4">
              <h2 className="font-semibold text-surface-900">Order Summary</h2>

              {/* Items */}
              <div className="space-y-3 max-h-64 overflow-y-auto pr-1">
                {items.map(item => (
                  <div key={item.id} className="flex items-center gap-3">
                    <div className="w-11 h-11 rounded-lg bg-white border border-surface-100 flex-shrink-0 overflow-hidden">
                      {item.productImageUrl
                        ? <ProductImage src={item.productImageUrl} alt={item.productName} className="w-full h-full object-contain p-1" />
                        : <div className="w-full h-full flex items-center justify-center text-surface-300 text-xs">IMG</div>}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-xs font-medium text-surface-900 truncate">{item.productName}</p>
                      <p className="text-xs text-surface-400">× {item.quantity}</p>
                    </div>
                    <span className="text-sm font-semibold flex-shrink-0">₦{item.lineTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                  </div>
                ))}
              </div>

              {/* Coupon */}
              <CouponInput
                onApply={handleCoupon}
                onRemove={removeCoupon}
                appliedCode={couponCode}
                discount={discount}
                loading={couponLoading}
              />

              {/* Totals */}
              <div className="border-t border-surface-100 pt-3 space-y-2 text-sm">
                <div className="flex justify-between text-surface-500">
                  <span>Subtotal</span>
                  <span>₦{total.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                {discount > 0 && (
                  <div className="flex justify-between text-emerald-600">
                    <span>Discount ({couponCode})</span>
                    <span>–₦{discount.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                  </div>
                )}
                <div className="flex justify-between text-surface-500">
                  <span>{pickup ? `Pickup${shop ? ` · ${shop.name}` : ''}` : 'Delivery'}</span>
                  <span>
                    {shipping === 0 ? <span className="text-emerald-600 font-medium">Free</span>
                      : shipping == null ? <span className="text-surface-400">Choose a state</span>
                      : naira(shipping)}
                  </span>
                </div>
                <div className="flex justify-between font-bold text-base pt-2 border-t border-surface-100">
                  <span>Total</span>
                  <span>₦{orderTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
              </div>

              <div className="flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-medium bg-amber-50 text-amber-700">
                <Building2 className="w-3.5 h-3.5" /> Paying by bank transfer
              </div>
            </div>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  )
}