import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Package } from 'lucide-react'
import toast from 'react-hot-toast'
import { authApi, cartApi, orderApi } from '../../services/api'
import { useAuthStore, useCartStore } from '../../store'
import { Input, Spinner } from '../../components/ui'
import { Navbar } from '../../components/layout/ShopLayout'

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
      // Load cart
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
            <div className="w-12 h-12 bg-brand-500 rounded-2xl flex items-center justify-center mx-auto mb-4">
              <Package className="w-6 h-6 text-white" />
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
            <p className="text-surface-500 text-sm mt-1">Join TechStore today</p>
          </div>
          <div className="card p-6">
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <Input label="First Name" placeholder="Jane"
                  error={errors.firstName?.message}
                  {...register('firstName', { required: 'Required' })} />
                <Input label="Last Name" placeholder="Doe"
                  error={errors.lastName?.message}
                  {...register('lastName', { required: 'Required' })} />
              </div>
              <Input label="Email" type="email" placeholder="you@example.com"
                error={errors.email?.message}
                {...register('email', { required: 'Email required', pattern: { value: /^\S+@\S+$/i, message: 'Invalid email' } })} />
              <Input label="Phone (optional)" type="tel" placeholder="+1 (416) 555-0100"
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

// ── Checkout ──────────────────────────────────────────────────────────────────
export function CheckoutPage() {
  const { register, handleSubmit, formState: { errors } } = useForm()
  const [loading, setLoading] = useState(false)
  const { items, total, clearLocal } = useCartStore()
  const navigate = useNavigate()

  const tax = +(total * 0.13).toFixed(2)
  const shipping = total >= 100 ? 0 : 9.99
  const orderTotal = +(total + tax + shipping).toFixed(2)

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      const { data: order } = await orderApi.checkout({
        shippingFirstName: data.firstName,
        shippingLastName: data.lastName,
        shippingAddress: data.address,
        shippingCity: data.city,
        shippingProvince: data.province,
        shippingPostalCode: data.postalCode,
        shippingCountry: 'Canada',
        shippingPhone: data.phone,
        paymentMethod: 'Card',
        notes: data.notes,
      })
      clearLocal()
      toast.success('Order placed!')
      navigate(`/account/orders/${order.id}`)
    } catch (err) {
      toast.error(err.response?.data?.message || 'Checkout failed')
    } finally { setLoading(false) }
  }

  if (!items.length) {
    navigate('/products')
    return null
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-1 page-container py-10">
        <h1 className="font-display text-2xl font-semibold mb-8">Checkout</h1>
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Form */}
          <form onSubmit={handleSubmit(onSubmit)} className="lg:col-span-2 space-y-6">
            <div className="card p-6">
              <h2 className="font-semibold mb-4">Shipping Address</h2>
              <div className="grid grid-cols-2 gap-4">
                <Input label="First Name" error={errors.firstName?.message}
                  {...register('firstName', { required: 'Required' })} />
                <Input label="Last Name" error={errors.lastName?.message}
                  {...register('lastName', { required: 'Required' })} />
                <Input label="Phone" className="col-span-2" type="tel"
                  {...register('phone')} />
                <Input label="Address" className="col-span-2" error={errors.address?.message}
                  {...register('address', { required: 'Required' })} />
                <Input label="City" error={errors.city?.message}
                  {...register('city', { required: 'Required' })} />
                <Input label="Province" error={errors.province?.message}
                  {...register('province', { required: 'Required' })} />
                <Input label="Postal Code" placeholder="A1B 2C3" error={errors.postalCode?.message}
                  {...register('postalCode', {
                    required: 'Required',
                    pattern: { value: /^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$/, message: 'Invalid Canadian postal code' }
                  })} />
              </div>
            </div>

            <div className="card p-6">
              <h2 className="font-semibold mb-4">Payment</h2>
              <div className="bg-surface-50 rounded-xl p-4 text-sm text-surface-500 text-center">
                🔒 Payment integration (Stripe, PayPal) would connect here
              </div>
            </div>

            <div className="card p-4">
              <Input label="Order Notes (optional)" as="textarea" rows={3}
                {...register('notes')} />
            </div>

            <button type="submit" disabled={loading} className="btn-primary w-full btn-lg">
              {loading ? <Spinner size="sm" /> : `Place Order · $${orderTotal.toFixed(2)}`}
            </button>
          </form>

          {/* Order summary */}
          <div>
            <div className="card p-6 sticky top-24">
              <h2 className="font-semibold mb-4">Order Summary</h2>
              <div className="space-y-3 mb-4">
                {items.map(item => (
                  <div key={item.id} className="flex justify-between text-sm">
                    <span className="text-surface-600 truncate mr-2">{item.productName} ×{item.quantity}</span>
                    <span className="font-medium flex-shrink-0">${item.lineTotal.toFixed(2)}</span>
                  </div>
                ))}
              </div>
              <div className="border-t border-surface-100 pt-3 space-y-2 text-sm">
                <div className="flex justify-between text-surface-500">
                  <span>Subtotal</span><span>${total.toFixed(2)}</span>
                </div>
                <div className="flex justify-between text-surface-500">
                  <span>HST (13%)</span><span>${tax.toFixed(2)}</span>
                </div>
                <div className="flex justify-between text-surface-500">
                  <span>Shipping</span>
                  <span>{shipping === 0 ? <span className="text-emerald-600">Free</span> : `$${shipping.toFixed(2)}`}</span>
                </div>
                <div className="flex justify-between font-semibold text-base pt-2 border-t border-surface-100">
                  <span>Total</span><span>${orderTotal.toFixed(2)}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}
