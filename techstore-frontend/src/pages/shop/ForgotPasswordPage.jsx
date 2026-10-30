import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Mail, ArrowLeft } from 'lucide-react'
import { authApi } from '../../services/api'
import { Input, Spinner } from '../../components/ui'
import { Navbar } from '../../components/layout/ShopLayout'
import { emailRules, showApiError } from '../../utils/validation'

export default function ForgotPasswordPage() {
  const { register, handleSubmit, setError, formState: { errors } } = useForm({ mode: 'onTouched' })
  const [loading, setLoading] = useState(false)
  const [sent, setSent] = useState(false)

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      await authApi.forgotPassword({ email: data.email.trim() })
      setSent(true)
    } catch (err) {
      showApiError(err, setError, 'Something went wrong. Please try again.')
    } finally { setLoading(false) }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <div className="flex-1 flex items-center justify-center p-4">
        <div className="w-full max-w-sm animate-fade-up">
          <Link to="/login" className="inline-flex items-center gap-1.5 text-sm text-surface-500 hover:text-surface-900 mb-6">
            <ArrowLeft className="w-4 h-4" /> Back to login
          </Link>

          {sent ? (
            <div className="card p-8 text-center space-y-3">
              <div className="w-14 h-14 rounded-2xl bg-emerald-50 flex items-center justify-center mx-auto">
                <Mail className="w-7 h-7 text-emerald-500" />
              </div>
              <h2 className="font-display font-semibold text-xl">Check your email</h2>
              <p className="text-surface-500 text-sm">
                If an account with that email exists, a password reset link has been sent. Check your inbox (and spam folder).
              </p>
              <Link to="/login" className="btn-primary w-full mt-4 inline-block text-center">
                Back to Login
              </Link>
            </div>
          ) : (
            <>
              <div className="text-center mb-8">
                <h1 className="font-display text-2xl font-semibold">Forgot password?</h1>
                <p className="text-surface-500 text-sm mt-1">Enter your email and we'll send a reset link</p>
              </div>
              <div className="card p-6">
                <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
                  <Input
                    label="Email Address"
                    type="email"
                    placeholder="you@example.com"
                    autoComplete="email"
                    error={errors.email?.message}
                    {...register('email', emailRules)}
                  />
                  <button type="submit" disabled={loading} className="btn-primary w-full btn-lg">
                    {loading ? <Spinner size="sm" /> : 'Send Reset Link'}
                  </button>
                </form>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
