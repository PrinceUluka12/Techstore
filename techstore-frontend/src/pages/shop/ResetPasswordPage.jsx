import { useState } from 'react'
import { Link, useSearchParams, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Lock, CheckCircle } from 'lucide-react'
import toast from 'react-hot-toast'
import { authApi } from '../../services/api'
import { Input, Spinner } from '../../components/ui'
import { Navbar } from '../../components/layout/ShopLayout'

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token')
  const { register, handleSubmit, watch, formState: { errors } } = useForm()
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)

  if (!token) {
    return (
      <div className="min-h-screen flex flex-col">
        <Navbar />
        <div className="flex-1 flex items-center justify-center p-4">
          <div className="text-center space-y-3">
            <p className="text-surface-500">Invalid or missing reset link.</p>
            <Link to="/forgot-password" className="btn-primary">Request a new link</Link>
          </div>
        </div>
      </div>
    )
  }

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      await authApi.resetPassword({ token, newPassword: data.newPassword })
      setDone(true)
      setTimeout(() => navigate('/login'), 3000)
    } catch (err) {
      toast.error(err.response?.data?.message || 'Reset failed. The link may have expired.')
    } finally { setLoading(false) }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <div className="flex-1 flex items-center justify-center p-4">
        <div className="w-full max-w-sm animate-fade-up">
          {done ? (
            <div className="card p-8 text-center space-y-3">
              <div className="w-14 h-14 rounded-2xl bg-emerald-50 flex items-center justify-center mx-auto">
                <CheckCircle className="w-7 h-7 text-emerald-500" />
              </div>
              <h2 className="font-display font-semibold text-xl">Password reset!</h2>
              <p className="text-surface-500 text-sm">Redirecting you to login…</p>
            </div>
          ) : (
            <>
              <div className="text-center mb-8">
                <h1 className="font-display text-2xl font-semibold">Set new password</h1>
                <p className="text-surface-500 text-sm mt-1">Choose a strong password for your account</p>
              </div>
              <div className="card p-6">
                <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                  <Input
                    label="New Password"
                    type="password"
                    placeholder="Min. 8 characters"
                    error={errors.newPassword?.message}
                    {...register('newPassword', {
                      required: 'Required',
                      minLength: { value: 8, message: 'At least 8 characters' },
                      pattern: { value: /(?=.*[A-Z])(?=.*[0-9])/, message: 'Must include uppercase and a number' },
                    })}
                  />
                  <Input
                    label="Confirm Password"
                    type="password"
                    placeholder="Repeat password"
                    error={errors.confirm?.message}
                    {...register('confirm', {
                      required: 'Required',
                      validate: v => v === watch('newPassword') || 'Passwords do not match',
                    })}
                  />
                  <button type="submit" disabled={loading} className="btn-primary w-full btn-lg">
                    {loading ? <Spinner size="sm" /> : <><Lock className="w-4 h-4" /> Reset Password</>}
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
