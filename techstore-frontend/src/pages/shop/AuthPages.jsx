import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import {
  Package, CreditCard, Building2, Lock,
  ChevronDown, ChevronUp, CheckCircle, Truck, Shield, Tag, X
} from 'lucide-react'
import toast from 'react-hot-toast'
import PaystackPop from '@paystack/inline-js'
import { authApi, cartApi, orderApi, paystackApi } from '../../services/api'
import { useAuthStore, useCartStore } from '../../store'
import { Input, Spinner } from '../../components/ui'
import { Navbar, Footer } from '../../components/layout/ShopLayout'

const NIGERIAN_STATES = [
  'Abia','Adamawa','Akwa Ibom','Anambra','Bauchi','Bayelsa','Benue','Borno',
  'Cross River','Delta','Ebonyi','Edo','Ekiti','Enugu','FCT - Abuja','Gombe',
  'Imo','Jigawa','Kaduna','Kano','Katsina','Kebbi','Kogi','Kwara','Lagos',
  'Nasarawa','Niger','Ogun','Ondo','Osun','Oyo','Plateau','Rivers','Sokoto',
  'Taraba','Yobe','Zamfara',
]

const PAYSTACK_PUBLIC_KEY = 'pk_live_6db9f9605582bc9866757429306bf6089eb4d7a4'

// ── Login ─────────────────────────────────────────────────────────────────────
export function LoginPage() {
  const { register, handleSubmit, formState: { errors } } = useForm()
  const [loading, setLoading] = useState(false)
  const { login } = useAuthStore()
  const { setCart } = useCartStore()
  const navigate = useNavigate()

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      const { data: auth } = await authApi.login(data)
      login(auth)
      try { const { data: cart } = await cartApi.get(); setCart(cart) } catch {}
      toast.success(`Welcome back, ${auth.firstName}!`)
      navigate(auth.role === 'Admin' ? '/admin' : '/')
    } catch (err) {
      toast.error(err.response?.data?.message || 'Login failed')
    } finally { setLoading(false) }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <div className="flex-1 flex items-center justify-center p-4">
        <div className="w-full max-w-sm animate-fade-up">
          <div className="text-center mb-8">
            <div className="w-16 h-16 flex items-center justify-center mx-auto mb-4">
              <img src="/hytel-logo.png" alt="Hytel Phones" className="w-full h-full object-contain" />
            </div>
            <h1 className="font-display text-2xl font-semibold">Welcome back</h1>
            <p className="text-surface-500 text-sm mt-1">Sign in to your account</p>
          </div>
          <div className="card p-6">
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <Input label="Email" type="email" placeholder="you@example.com"
                error={errors.email?.message}
                {...register('email', { required: 'Email is required', pattern: { value: /^\S+@\S+$/i, message: 'Invalid email' } })} />
              <Input label="Password" type="password" placeholder="••••••••"
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
            <Link to="/register" className="text-brand-600 font-medium hover:underline">Create one</Link>
          </p>
        </div>
      </div>
    </div>
  )
}

// ── Register ──────────────────────────────────────────────────────────────────
export function RegisterPage() {
  const { register, handleSubmit, watch, formState: { errors } } = useForm()
  const [loading, setLoading] = useState(false)
  const { login } = useAuthStore()
  const navigate = useNavigate()

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      const { data: auth } = await authApi.register({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        password: data.password,
        phone: data.phone,
      })
      login(auth)
      toast.success(`Welcome, ${auth.firstName}!`)
      navigate('/')
    } catch (err) {
      toast.error(err.response?.data?.message || 'Registration failed')
    } finally { setLoading(false) }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <div className="flex-1 flex items-center justify-center p-4 py-12">
        <div className="w-full max-w-sm animate-fade-up">
          <div className="text-center mb-8">
            <h1 className="font-display text-2xl font-semibold">Create account</h1>
            <p className="text-surface-500 text-sm mt-1">Join Hytel Phones today</p>
          </div>
          <div className="card p-6">
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="grid grid-cols-1 xs:grid-cols-2 gap-3">
                <Input label="First Name" placeholder="Jane" error={errors.firstName?.message}
                  {...register('firstName', { required: 'Required' })} />
                <Input label="Last Name" placeholder="Doe" error={errors.lastName?.message}
                  {...register('lastName', { required: 'Required' })} />
              </div>
              <Input label="Email" type="email" placeholder="you@example.com"
                error={errors.email?.message}
                {...register('email', { required: 'Email required', pattern: { value: /^\S+@\S+$/i, message: 'Invalid email' } })} />
              <Input label="Phone (optional)" type="tel" placeholder="+234 800 000 0000"
                {...register('phone')} />
              <Input label="Password" type="password" placeholder="Min. 8 characters"
                error={errors.password?.message}
                {...register('password', {
                  required: 'Password required',
                  minLength: { value: 8, message: 'At least 8 characters' },
                  pattern: { value: /(?=.*[A-Z])(?=.*[0-9])/, message: 'Must include uppercase and number' }
                })} />
              <Input label="Confirm Password" type="password" placeholder="Repeat password"
                error={errors.confirmPassword?.message}
                {...register('confirmPassword', {
                  required: 'Please confirm',
                  validate: v => v === watch('password') || 'Passwords do not match'
                })} />
              <button type="submit" disabled={loading} className="btn-primary w-full btn-lg">
                {loading ? <Spinner size="sm" /> : 'Create Account'}
              </button>
            </form>
          </div>
          <p className="text-center text-sm text-surface-500 mt-4">
            Already have an account?{' '}
            <Link to="/login" className="text-brand-600 font-medium hover:underline">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  )
}

// ── Payment method selector ───────────────────────────────────────────────────
function PaymentSelector({ selected, onChange }) {
  const options = [
    {
      id: 'Card',
      label: 'Debit / Credit Card',
      icon: CreditCard,
      desc: 'Visa, Mastercard, Verve',
    },
    {
      id: 'BankTransfer',
      label: 'Bank Transfer',
      icon: Building2,
      desc: 'Direct bank payment',
    },
  ]

  return (
    <div className="grid grid-cols-1 xs:grid-cols-2 gap-3">
      {options.map(opt => (
        <button key={opt.id} type="button" onClick={() => onChange(opt.id)}
          className={`flex items-center gap-3 p-4 rounded-xl border-2 text-left transition-all duration-150
            ${selected === opt.id
              ? 'border-brand-500 bg-brand-50'
              : 'border-surface-200 hover:border-surface-300 bg-white'}`}>
          <div className={`w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0 transition-colors
            ${selected === opt.id ? 'bg-brand-500' : 'bg-surface-100'}`}>
            <opt.icon className={`w-5 h-5 ${selected === opt.id ? 'text-white' : 'text-surface-500'}`} />
          </div>
          <div className="min-w-0">
            <p className={`text-sm font-semibold leading-tight ${selected === opt.id ? 'text-brand-700' : 'text-surface-900'}`}>
              {opt.label}
            </p>
            <p className={`text-xs mt-0.5 ${selected === opt.id ? 'text-brand-500' : 'text-surface-400'}`}>
              {opt.desc}
            </p>
          </div>
          {selected === opt.id && (
            <CheckCircle className="w-4 h-4 text-brand-500 ml-auto flex-shrink-0" />
          )}
        </button>
      ))}
    </div>
  )
}

// ── Card details form ─────────────────────────────────────────────────────────
function CardDetails({ register, errors }) {
  const [showCvv, setShowCvv] = useState(false)

  // Format card number with spaces
  const formatCard = (e) => {
    let v = e.target.value.replace(/\D/g, '').substring(0, 16)
    e.target.value = v.replace(/(.{4})/g, '$1 ').trim()
  }

  // Format expiry MM/YY
  const formatExpiry = (e) => {
    let v = e.target.value.replace(/\D/g, '').substring(0, 4)
    if (v.length >= 2) v = v.slice(0, 2) + '/' + v.slice(2)
    e.target.value = v
  }

  return (
    <div className="space-y-4 mt-4 pt-4 border-t border-surface-100">
      <Input
        label="Cardholder Name"
        placeholder="Name on card"
        error={errors.cardName?.message}
        {...register('cardName', { required: 'Required' })}
      />
      <div>
        <label className="label">Card Number</label>
        <div className="relative">
          <input
            className="input pr-12"
            placeholder="0000 0000 0000 0000"
            maxLength={19}
            onInput={formatCard}
            {...register('cardNumber', {
              required: 'Required',
              minLength: { value: 19, message: 'Enter a valid 16-digit card number' },
            })}
          />
          <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1">
            <div className="w-6 h-4 rounded-sm bg-blue-600 opacity-80" />
            <div className="w-6 h-4 rounded-sm bg-red-500 opacity-80 -ml-2" />
          </div>
        </div>
        {errors.cardNumber && <p className="text-red-500 text-xs mt-1">{errors.cardNumber.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="label">Expiry Date</label>
          <input
            className="input"
            placeholder="MM/YY"
            maxLength={5}
            onInput={formatExpiry}
            {...register('cardExpiry', {
              required: 'Required',
              pattern: { value: /^(0[1-9]|1[0-2])\/\d{2}$/, message: 'Use MM/YY format' },
            })}
          />
          {errors.cardExpiry && <p className="text-red-500 text-xs mt-1">{errors.cardExpiry.message}</p>}
        </div>
        <div>
          <label className="label flex items-center gap-1.5">
            CVV
            <span className="text-[10px] text-surface-400 font-normal">3–4 digits on back</span>
          </label>
          <div className="relative">
            <input
              className="input pr-10"
              placeholder="•••"
              maxLength={4}
              type={showCvv ? 'text' : 'password'}
              {...register('cardCvv', {
                required: 'Required',
                minLength: { value: 3, message: 'Min 3 digits' },
              })}
            />
            <button type="button" onClick={() => setShowCvv(v => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-surface-400 hover:text-surface-600 text-xs">
              {showCvv ? 'Hide' : 'Show'}
            </button>
          </div>
          {errors.cardCvv && <p className="text-red-500 text-xs mt-1">{errors.cardCvv.message}</p>}
        </div>
      </div>

      <div className="flex items-center gap-2 text-xs text-surface-400 bg-surface-50 rounded-xl px-3 py-2.5">
        <Lock className="w-3.5 h-3.5 text-emerald-500 flex-shrink-0" />
        Your card details are encrypted and never stored on our servers.
      </div>
    </div>
  )
}

// ── Bank transfer details ─────────────────────────────────────────────────────
function BankTransferDetails({ orderTotal }) {
  const [copied, setCopied] = useState(null)

  const bankDetails = [
    { label: 'Bank',           value: 'First Bank Nigeria' },
    { label: 'Account Name',   value: 'Hytel Phones Nigeria Ltd' },
    { label: 'Account Number', value: '3012345678', copyable: true },
    { label: 'Sort Code',      value: '011', copyable: false },
  ]

  const copy = (val, key) => {
    navigator.clipboard.writeText(val)
    setCopied(key)
    toast.success('Copied!')
    setTimeout(() => setCopied(null), 2000)
  }

  return (
    <div className="space-y-4 mt-4 pt-4 border-t border-surface-100">
      <div className="bg-amber-50 border border-amber-200 rounded-xl p-4">
        <p className="text-sm font-semibold text-amber-800 mb-1">How it works</p>
        <ol className="text-xs text-amber-700 space-y-1 list-decimal list-inside">
          <li>Transfer <strong>₦{orderTotal.toLocaleString('en-NG', { minimumFractionDigits: 2 })}</strong> to the account below</li>
          <li>Use your name as the payment reference</li>
          <li>Your order will be confirmed within 1–2 business hours</li>
        </ol>
      </div>

      <div className="rounded-xl border border-surface-200 overflow-hidden">
        {bankDetails.map((row, i) => (
          <div key={row.label}
            className={`flex items-center justify-between px-4 py-3 text-sm ${i % 2 === 0 ? 'bg-surface-50' : 'bg-white'} ${i > 0 ? 'border-t border-surface-100' : ''}`}>
            <span className="text-surface-500 flex-shrink-0 w-28 sm:w-36">{row.label}</span>
            <div className="flex items-center gap-2">
              <span className="font-semibold text-surface-900">{row.value}</span>
              {row.copyable && (
                <button type="button" onClick={() => copy(row.value, row.label)}
                  className={`text-xs px-2 py-0.5 rounded-md transition-colors ${copied === row.label ? 'bg-emerald-100 text-emerald-700' : 'bg-surface-100 text-surface-600 hover:bg-brand-50 hover:text-brand-600'}`}>
                  {copied === row.label ? '✓ Copied' : 'Copy'}
                </button>
              )}
            </div>
          </div>
        ))}
      </div>

      <Input
        label="Transfer Reference / Sender Name"
        placeholder="e.g. John Doe — used to match your payment"
        {...({} /* not validated — bank transfer confirmed manually */)}
        name="transferRef"
      />

      <div className="flex items-center gap-2 text-xs text-surface-400 bg-surface-50 rounded-xl px-3 py-2.5">
        <Building2 className="w-3.5 h-3.5 text-brand-500 flex-shrink-0" />
        Orders are processed after payment is confirmed by our team.
      </div>
    </div>
  )
}

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
export function CheckoutPage() {
  const { register, handleSubmit, formState: { errors } } = useForm()
  const [loading, setLoading]               = useState(false)
  const [paymentMethod, setPaymentMethod]   = useState('Card')
  const { items, total, clearLocal, applyCoupon, removeCoupon, couponCode, discountAmount, couponLoading } = useCartStore()
  const { user } = useAuthStore()
  const navigate = useNavigate()

  const discount   = discountAmount || 0
  const tax        = +((total - discount) * 0.075).toFixed(2)
  const shipping   = total >= 100 ? 0 : 9.99
  const orderTotal = +(total - discount + tax + shipping).toFixed(2)

  const handleCoupon = async (code) => {
    const result = await applyCoupon(code)
    if (result.success) toast.success(`Coupon applied! –₦${result.discountAmount.toLocaleString('en-NG', { minimumFractionDigits: 2 })}`)
    else toast.error(result.error || 'Invalid coupon')
  }

  const buildOrderPayload = (data) => ({
    shippingFirstName:  data.firstName,
    shippingLastName:   data.lastName,
    shippingAddress:    data.address,
    shippingCity:       data.city,
    shippingProvince:   data.province,
    shippingPostalCode: data.postalCode,
    shippingCountry:    'Nigeria',
    shippingPhone:      data.phone,
    paymentMethod,
    notes:              data.notes,
    couponCode:         couponCode || null,
  })

  const openPaystack = (orderId, amount, email) => {
    const popup = new PaystackPop()
    popup.newTransaction({
      key:      PAYSTACK_PUBLIC_KEY,
      email:    email || user?.email,
      amount:   Math.round(amount * 100),
      metadata: { order_id: orderId },
      onSuccess: async (transaction) => {
        try {
          await paystackApi.verify(transaction.reference)
          clearLocal()
          toast.success('Payment confirmed! Order is being processed.')
        } catch {
          toast.success('Order placed. Payment verification in progress.')
        }
        navigate(`/account/orders/${orderId}`)
      },
      onCancel: () => {
        toast('Payment cancelled. Your order is saved — you can pay later from your orders page.', { icon: 'ℹ️' })
        clearLocal()
        navigate(`/account/orders/${orderId}`)
      },
    })
  }

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      const { data: order } = await orderApi.checkout(buildOrderPayload(data))

      if (paymentMethod === 'BankTransfer') {
        clearLocal()
        toast.success('Order placed! Please complete your bank transfer.')
        navigate(`/account/orders/${order.id}`)
      } else {
        setLoading(false)
        openPaystack(order.id, order.total, data.email || user?.email)
      }
    } catch (err) {
      toast.error(err.response?.data?.message || 'Checkout failed')
      setLoading(false)
    }
  }

  if (!items.length) {
    navigate('/products')
    return null
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
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
          <form onSubmit={handleSubmit(onSubmit)} className="lg:col-span-2 space-y-5">

            {/* Shipping */}
            <div className="card p-6">
              <div className="flex items-center gap-2 mb-5">
                <div className="w-7 h-7 rounded-lg bg-brand-50 flex items-center justify-center">
                  <Truck className="w-3.5 h-3.5 text-brand-600" />
                </div>
                <h2 className="font-semibold text-surface-900">Shipping Address</h2>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <Input label="First Name" error={errors.firstName?.message}
                  {...register('firstName', { required: 'Required' })} />
                <Input label="Last Name" error={errors.lastName?.message}
                  {...register('lastName', { required: 'Required' })} />
                <Input label="Phone Number" className="col-span-2" type="tel"
                  placeholder="+234 800 000 0000" {...register('phone')} />
                <Input label="Street Address" className="col-span-2" error={errors.address?.message}
                  placeholder="House number, street name"
                  {...register('address', { required: 'Required' })} />
                <Input label="City" error={errors.city?.message}
                  {...register('city', { required: 'Required' })} />
                <div>
                  <label className="label">State {errors.province && <span className="text-red-500 text-xs ml-1">{errors.province.message}</span>}</label>
                  <select className="input w-full" {...register('province', { required: 'Required' })}>
                    <option value="">Select state…</option>
                    {NIGERIAN_STATES.map(s => <option key={s} value={s}>{s}</option>)}
                  </select>
                </div>
                <Input label="Postal Code" placeholder="100001" error={errors.postalCode?.message}
                  {...register('postalCode', { required: 'Required' })} />
              </div>
            </div>

            {/* Payment */}
            <div className="card p-6">
              <div className="flex items-center gap-2 mb-5">
                <div className="w-7 h-7 rounded-lg bg-brand-50 flex items-center justify-center">
                  <CreditCard className="w-3.5 h-3.5 text-brand-600" />
                </div>
                <h2 className="font-semibold text-surface-900">Payment Method</h2>
              </div>

              <PaymentSelector selected={paymentMethod} onChange={setPaymentMethod} />

              {paymentMethod === 'Card' && (
                <div className="mt-4 pt-4 border-t border-surface-100">
                  <div className="flex items-center gap-2 text-sm text-surface-600 bg-brand-50 rounded-xl px-3 py-2.5">
                    <Lock className="w-3.5 h-3.5 text-brand-500 flex-shrink-0" />
                    You'll be redirected to Paystack's secure checkout to complete payment.
                  </div>
                </div>
              )}
              {paymentMethod === 'BankTransfer' && (
                <BankTransferDetails orderTotal={orderTotal} />
              )}
            </div>

            {/* Notes */}
            <div className="card p-5">
              <label className="label">Order Notes <span className="text-surface-400 font-normal">(optional)</span></label>
              <textarea className="input min-h-[80px] resize-none" placeholder="Special delivery instructions…"
                {...register('notes')} />
            </div>

            {/* Place order */}
            <button type="submit" disabled={loading}
              className="btn-primary w-full btn-lg text-base font-bold gap-3">
              {loading
                ? <><Spinner size="sm" /> Processing…</>
                : <>
                    <Lock className="w-4 h-4" />
                    {paymentMethod === 'BankTransfer'
                      ? 'Place Order — Pay via Transfer'
                      : `Pay ₦${orderTotal.toLocaleString('en-NG', { minimumFractionDigits: 2 })} via Paystack`}
                  </>}
            </button>

            <p className="text-center text-xs text-surface-400 flex items-center justify-center gap-1.5">
              <Shield className="w-3.5 h-3.5 text-emerald-500" />
              Your order is protected by Hytel Phones' buyer guarantee
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
                        ? <img src={item.productImageUrl} alt={item.productName} className="w-full h-full object-contain p-1" />
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
                  <span>VAT (7.5%)</span>
                  <span>₦{tax.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between text-surface-500">
                  <span>Shipping</span>
                  <span>
                    {shipping === 0
                      ? <span className="text-emerald-600 font-medium">Free</span>
                      : `₦${Number(shipping).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}
                  </span>
                </div>
                <div className="flex justify-between font-bold text-base pt-2 border-t border-surface-100">
                  <span>Total</span>
                  <span>₦{orderTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
              </div>

              <div className={`flex items-center gap-2 px-3 py-2 rounded-xl text-xs font-medium
                ${paymentMethod === 'Card' ? 'bg-brand-50 text-brand-700' : 'bg-amber-50 text-amber-700'}`}>
                {paymentMethod === 'Card'
                  ? <><CreditCard className="w-3.5 h-3.5" /> Paying via Paystack</>
                  : <><Building2 className="w-3.5 h-3.5" /> Paying by bank transfer</>}
              </div>
            </div>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  )
}