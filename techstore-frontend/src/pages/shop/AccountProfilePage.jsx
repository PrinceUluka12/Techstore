import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { User, Lock, Heart } from 'lucide-react'
import toast from 'react-hot-toast'
import { authApi } from '../../services/api'
import { useAuthStore, useWishlistStore } from '../../store'
import { Input, Spinner, LoadingPage } from '../../components/ui'
import { Navbar, Footer } from '../../components/layout/ShopLayout'
import { Link } from 'react-router-dom'

function ProfileForm({ profile, onSaved }) {
  const { register, handleSubmit, formState: { errors, isDirty } } = useForm({
    defaultValues: {
      firstName: profile.firstName,
      lastName:  profile.lastName,
      phone:     profile.phone ?? '',
      address:   profile.address ?? '',
    }
  })
  const { updateUser } = useAuthStore()
  const [saving, setSaving] = useState(false)

  const onSubmit = async (data) => {
    setSaving(true)
    try {
      const { data: updated } = await authApi.updateMe(data)
      updateUser({ firstName: updated.firstName, lastName: updated.lastName })
      toast.success('Profile updated')
      onSaved(updated)
    } catch {
      toast.error('Failed to update profile')
    } finally { setSaving(false) }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Input label="First Name" error={errors.firstName?.message}
          {...register('firstName', { required: 'Required' })} />
        <Input label="Last Name" error={errors.lastName?.message}
          {...register('lastName', { required: 'Required' })} />
      </div>
      <Input label="Phone" type="tel" placeholder="+234 800 000 0000" {...register('phone')} />
      <div>
        <label className="label">Delivery Address <span className="text-surface-400 font-normal">(optional)</span></label>
        <textarea className="input min-h-[80px] resize-none" placeholder="Your default delivery address…"
          {...register('address')} />
      </div>
      <div className="flex justify-end">
        <button type="submit" disabled={saving || !isDirty} className="btn-primary">
          {saving ? <Spinner size="sm" /> : 'Save Changes'}
        </button>
      </div>
    </form>
  )
}

function ChangePasswordForm() {
  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm()
  const [saving, setSaving] = useState(false)

  const onSubmit = async (data) => {
    setSaving(true)
    try {
      await authApi.changePassword({ currentPassword: data.current, newPassword: data.newPass })
      toast.success('Password changed. Please log in again on other devices.')
      reset()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to change password')
    } finally { setSaving(false) }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <Input label="Current Password" type="password" error={errors.current?.message}
        {...register('current', { required: 'Required' })} />
      <Input label="New Password" type="password" placeholder="Min. 8 characters"
        error={errors.newPass?.message}
        {...register('newPass', {
          required: 'Required',
          minLength: { value: 8, message: 'At least 8 characters' },
          pattern: { value: /(?=.*[A-Z])(?=.*[0-9])/, message: 'Must include uppercase and a number' },
        })} />
      <Input label="Confirm New Password" type="password" error={errors.confirm?.message}
        {...register('confirm', {
          required: 'Required',
          validate: v => v === watch('newPass') || 'Passwords do not match',
        })} />
      <div className="flex justify-end">
        <button type="submit" disabled={saving} className="btn-primary">
          {saving ? <Spinner size="sm" /> : 'Change Password'}
        </button>
      </div>
    </form>
  )
}

export default function AccountProfilePage() {
  const { user } = useAuthStore()
  const { items: wishlistItems, fetch: fetchWishlist } = useWishlistStore()
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    authApi.getMe()
      .then(r => setProfile(r.data))
      .finally(() => setLoading(false))
    fetchWishlist()
  }, [])

  if (loading) return <><Navbar /><LoadingPage /></>

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-1 page-container py-10">
        <h1 className="font-display text-2xl font-semibold mb-1">My Account</h1>
        <p className="text-surface-400 text-sm mb-8">
          {user?.email} · Member since {profile ? new Date(profile.createdAt).getFullYear() : ''}
        </p>

        <div className="grid lg:grid-cols-3 gap-6">
          {/* Left nav */}
          <nav className="space-y-1">
            {[
              { label: 'Profile', icon: User, href: '/account/profile', active: true },
              { label: 'My Orders', icon: null, href: '/account/orders' },
              { label: 'Wishlist', icon: Heart, href: '/account/wishlist', badge: wishlistItems.length || null },
            ].map(item => (
              <Link key={item.href} to={item.href}
                className={`flex items-center gap-3 px-4 py-2.5 rounded-xl text-sm transition-colors
                  ${item.active ? 'bg-brand-50 text-brand-700 font-medium' : 'text-surface-600 hover:bg-surface-50'}`}>
                {item.icon && <item.icon className="w-4 h-4" />}
                {item.label}
                {item.badge && (
                  <span className="ml-auto text-xs bg-brand-500 text-white rounded-full w-5 h-5 flex items-center justify-center">{item.badge}</span>
                )}
              </Link>
            ))}
          </nav>

          {/* Right content */}
          <div className="lg:col-span-2 space-y-6">
            <div className="card p-6">
              <div className="flex items-center gap-2 mb-5">
                <User className="w-4 h-4 text-brand-500" />
                <h2 className="font-semibold">Personal Information</h2>
              </div>
              {profile && <ProfileForm profile={profile} onSaved={setProfile} />}
            </div>

            <div className="card p-6">
              <div className="flex items-center gap-2 mb-5">
                <Lock className="w-4 h-4 text-brand-500" />
                <h2 className="font-semibold">Change Password</h2>
              </div>
              <ChangePasswordForm />
            </div>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  )
}
