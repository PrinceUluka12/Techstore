import { useEffect, useState, useCallback } from 'react'
import { useForm } from 'react-hook-form'
import { AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import { UserCheck, UserX, TrendingUp, Calendar, UserPlus, X, Shield } from 'lucide-react'
import { adminApi, roleApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Spinner, Pagination } from '../../components/ui'
import { Input } from '../../components/ui'
import toast from 'react-hot-toast'
import { format, subDays } from 'date-fns'

function useRoles() {
  const [roles, setRoles] = useState([])
  useEffect(() => {
    roleApi.getAll()
      .then(r => setRoles(r.data.map(role => role.name)))
      .catch(() => setRoles(['Customer', 'Admin']))
  }, [])
  return roles
}

// ── Create Staff Modal ────────────────────────────────────────────────────────
function CreateStaffModal({ onClose, onCreated }) {
  const { register, handleSubmit, formState: { errors } } = useForm({ defaultValues: { role: 'Admin' } })
  const [saving, setSaving] = useState(false)
  const roles = useRoles()

  const onSubmit = async (data) => {
    setSaving(true)
    try {
      await adminApi.createStaff(data)
      toast.success(`${data.role} account created for ${data.firstName}`)
      onCreated()
      onClose()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to create account')
    } finally { setSaving(false) }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative bg-white rounded-2xl shadow-xl w-full max-w-md animate-fade-up">
        <div className="flex items-center justify-between p-6 border-b border-surface-100">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-brand-50 flex items-center justify-center">
              <UserPlus className="w-4 h-4 text-brand-600" />
            </div>
            <div>
              <h2 className="font-display font-semibold text-surface-900">Create Back Office Account</h2>
              <p className="text-xs text-surface-400 mt-0.5">Account will be created with no order history</p>
            </div>
          </div>
          <button onClick={onClose} className="btn-icon"><X className="w-4 h-4" /></button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="p-6 space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input label="First Name" placeholder="John"
              error={errors.firstName?.message}
              {...register('firstName', { required: 'Required' })} />
            <Input label="Last Name" placeholder="Doe"
              error={errors.lastName?.message}
              {...register('lastName', { required: 'Required' })} />
          </div>

          <Input label="Email Address" type="email" placeholder="staff@pgusolutions.com"
            error={errors.email?.message}
            {...register('email', {
              required: 'Required',
              pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: 'Invalid email' }
            })} />

          <Input label="Temporary Password" type="password" placeholder="Min. 8 characters"
            error={errors.password?.message}
            {...register('password', {
              required: 'Required',
              minLength: { value: 8, message: 'At least 8 characters' },
              pattern: { value: /(?=.*[A-Z])(?=.*[0-9])/, message: 'Must include uppercase and a number' },
            })} />

          <div>
            <label className="label">Role</label>
            <select className="input text-sm" {...register('role', { required: true })}>
              {roles.map(r => <option key={r} value={r}>{r}</option>)}
            </select>
            <p className="text-xs text-surface-400 mt-1.5">
              Admin has full access. Custom roles have only the permissions assigned to them.
            </p>
          </div>

          <div className="flex gap-3 pt-2">
            <button type="button" onClick={onClose} className="btn-secondary flex-1">Cancel</button>
            <button type="submit" disabled={saving} className="btn-primary flex-1">
              {saving ? <Spinner size="sm" /> : 'Create Account'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Role selector cell ────────────────────────────────────────────────────────
function RoleCell({ user, allRoles, onChanged }) {
  const [loading, setLoading] = useState(false)

  const handleChange = async (e) => {
    const newRole = e.target.value
    if (newRole === user.role) return
    if (!confirm(`Change ${user.firstName}'s role from ${user.role} to ${newRole}? Their current session will be signed out.`)) return
    setLoading(true)
    try {
      await adminApi.assignRole(user.id, newRole)
      toast.success(`Role updated to ${newRole}`)
      onChanged()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to update role')
    } finally { setLoading(false) }
  }

  return (
    <div className="flex items-center gap-2">
      {loading ? <Spinner size="sm" /> : (
        <select
          value={user.role}
          onChange={handleChange}
          className={`text-xs font-medium rounded-lg px-2 py-1 border focus:outline-none focus:ring-2 focus:ring-brand-500 cursor-pointer
            ${user.role === 'Admin'
              ? 'bg-brand-50 text-brand-700 border-brand-200'
              : 'bg-surface-100 text-surface-600 border-surface-200'}`}>
          {allRoles.map(r => <option key={r} value={r}>{r}</option>)}
        </select>
      )}
      {user.role === 'Admin' && <Shield className="w-3 h-3 text-brand-500" />}
    </div>
  )
}

// ── Users Page ────────────────────────────────────────────────────────────────
export function AdminUsers() {
  const [users, setUsers] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [showCreate, setShowCreate] = useState(false)
  const allRoles = useRoles()
  const pageSize = 20
  const totalPages = Math.ceil(total / pageSize)

  const load = useCallback(() => {
    setLoading(true)
    adminApi.getUsers({ page, pageSize })
      .then(r => { setUsers(r.data.items); setTotal(r.data.total) })
      .finally(() => setLoading(false))
  }, [page])

  useEffect(() => { load() }, [load])

  const toggleUser = async (userId, name, isActive) => {
    if (!confirm(`${isActive ? 'Deactivate' : 'Activate'} ${name}?`)) return
    try {
      await adminApi.toggleUser(userId)
      toast.success(`User ${isActive ? 'deactivated' : 'activated'}`)
      load()
    } catch { toast.error('Action failed') }
  }

  return (
    <AdminLayout>
      {showCreate && (
        <CreateStaffModal onClose={() => setShowCreate(false)} onCreated={load} />
      )}

      <div className="space-y-6 max-w-[1400px]">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h1 className="font-display text-2xl font-semibold">Users</h1>
            <p className="text-surface-400 text-sm mt-0.5">{total} registered accounts</p>
          </div>
          <button onClick={() => setShowCreate(true)} className="btn-primary gap-2">
            <UserPlus className="w-4 h-4" /> Create Back Office Account
          </button>
        </div>

        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>User</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Role</th>
                <th>Orders</th>
                <th>Total Spent</th>
                <th>Status</th>
                <th>Joined</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={9} className="text-center py-12"><Spinner /></td></tr>
              ) : users.length === 0 ? (
                <tr><td colSpan={9} className="text-center py-12 text-surface-400">No users found</td></tr>
              ) : users.map(u => (
                <tr key={u.id}>
                  <td>
                    <div className="flex items-center gap-2.5">
                      <div className={`w-8 h-8 rounded-full flex items-center justify-center text-white text-xs font-semibold flex-shrink-0
                        ${u.role === 'Admin' ? 'bg-brand-500' : 'bg-surface-400'}`}>
                        {u.firstName?.[0]}{u.lastName?.[0]}
                      </div>
                      <span className="font-medium text-surface-900">{u.firstName} {u.lastName}</span>
                    </div>
                  </td>
                  <td><span className="text-sm text-surface-600">{u.email}</span></td>
                  <td><span className="text-sm text-surface-500">{u.phone || '—'}</span></td>
                  <td><RoleCell user={u} allRoles={allRoles} onChanged={load} /></td>
                  <td><span className="text-sm font-medium">{u.totalOrders}</span></td>
                  <td><span className="text-sm font-semibold text-surface-900">₦{u.totalSpent.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span></td>
                  <td>
                    <span className={`badge ${u.isActive ? 'badge-green' : 'badge-red'}`}>
                      {u.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td><span className="text-xs text-surface-400">{format(new Date(u.createdAt), 'MMM d, yyyy')}</span></td>
                  <td>
                    <button onClick={() => toggleUser(u.id, `${u.firstName} ${u.lastName}`, u.isActive)}
                      className={`btn-icon ${u.isActive ? 'text-red-400 hover:bg-red-50' : 'text-emerald-500 hover:bg-emerald-50'}`}
                      title={u.isActive ? 'Deactivate' : 'Activate'}>
                      {u.isActive ? <UserX className="w-3.5 h-3.5" /> : <UserCheck className="w-3.5 h-3.5" />}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <Pagination page={page} totalPages={totalPages} onChange={setPage} />
      </div>
    </AdminLayout>
  )
}

// ── Reports Page ──────────────────────────────────────────────────────────────
export function AdminReports() {
  const [report, setReport] = useState(null)
  const [loading, setLoading] = useState(false)
  const [from, setFrom] = useState(format(subDays(new Date(), 30), 'yyyy-MM-dd'))
  const [to, setTo] = useState(format(new Date(), 'yyyy-MM-dd'))

  const load = () => {
    setLoading(true)
    adminApi.salesReport({ from, to })
      .then(r => setReport(r.data))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  return (
    <AdminLayout>
      <div className="space-y-8 max-w-[1400px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Sales Reports</h1>
            <p className="text-surface-400 text-sm mt-0.5">Revenue and performance analytics</p>
          </div>
          <div className="flex flex-wrap items-center gap-2 sm:gap-3">
            <div className="flex items-center gap-2">
              <Calendar className="w-4 h-4 text-surface-400" />
              <input type="date" className="input text-sm w-auto" value={from} onChange={e => setFrom(e.target.value)} />
              <span className="text-surface-400 text-sm">to</span>
              <input type="date" className="input text-sm w-auto" value={to} onChange={e => setTo(e.target.value)} />
            </div>
            <button onClick={load} disabled={loading} className="btn-primary">
              {loading ? <Spinner size="sm" /> : 'Run Report'}
            </button>
          </div>
        </div>

        {loading && <div className="flex justify-center py-16"><Spinner size="xl" /></div>}

        {report && !loading && (
          <>
            {/* KPI summary */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                { label: 'Total Orders', value: report.totalOrders?.toLocaleString() ?? '0' },
                { label: 'Total Revenue', value: `₦${(report.totalRevenue ?? 0).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` },
                { label: 'Avg Order Value', value: `₦${(report.averageOrderValue ?? 0).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` },
                { label: 'Date Range', value: `${format(new Date(report.fromDate), 'MMM d')} – ${format(new Date(report.toDate), 'MMM d')}` },
              ].map(k => (
                <div key={k.label} className="card p-5">
                  <p className="text-xs text-surface-400 uppercase tracking-wider mb-1">{k.label}</p>
                  <p className="font-display text-2xl font-semibold text-surface-900">{k.value}</p>
                </div>
              ))}
            </div>

            {/* Revenue chart */}
            <div className="card p-6">
              <h2 className="font-display font-semibold mb-5 flex items-center gap-2">
                <TrendingUp className="w-4 h-4 text-brand-500" /> Daily Revenue
              </h2>
              <ResponsiveContainer width="100%" height={280}>
                <AreaChart data={report.dailyBreakdown ?? []} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
                  <defs>
                    <linearGradient id="repGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#2d52ff" stopOpacity={0.15} />
                      <stop offset="95%" stopColor="#2d52ff" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f4f4f5" />
                  <XAxis dataKey="date" tick={{ fontSize: 11, fill: '#a1a1aa' }}
                    tickFormatter={d => d.slice(5)} tickLine={false} axisLine={false} />
                  <YAxis tick={{ fontSize: 11, fill: '#a1a1aa' }} tickLine={false} axisLine={false}
                    tickFormatter={v => `₦${v}`} />
                  <Tooltip contentStyle={{ borderRadius: 12, border: 'none', fontSize: 13 }}
                    formatter={v => [`₦${Number(v).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, 'Revenue']} />
                  <Area type="monotone" dataKey="revenue" stroke="#2d52ff" strokeWidth={2}
                    fill="url(#repGrad)" dot={false} activeDot={{ r: 4, strokeWidth: 0 }} />
                </AreaChart>
              </ResponsiveContainer>
            </div>

            {/* Top products */}
            <div className="grid lg:grid-cols-2 gap-6">
              <div className="card p-6">
                <h2 className="font-display font-semibold mb-5">Top Products by Revenue</h2>
                <ResponsiveContainer width="100%" height={240}>
                  <BarChart data={(report.topProducts ?? []).slice(0, 6)} layout="vertical" margin={{ left: 0, right: 20 }}>
                    <XAxis type="number" tick={{ fontSize: 11, fill: '#a1a1aa' }} tickLine={false} axisLine={false}
                      tickFormatter={v => `₦${v}`} />
                    <YAxis dataKey="productName" type="category" tick={{ fontSize: 10, fill: '#71717a' }}
                      width={90} tickLine={false} axisLine={false}
                      tickFormatter={v => v.length > 13 ? v.slice(0, 13) + '…' : v} />
                    <Tooltip contentStyle={{ borderRadius: 10, border: 'none', fontSize: 12 }}
                      formatter={v => [`₦${Number(v).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`, 'Revenue']} />
                    <Bar dataKey="revenue" fill="#2d52ff" radius={[0, 6, 6, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>

              <div className="card p-6">
                <h2 className="font-display font-semibold mb-5">Top Products by Units Sold</h2>
                <div className="space-y-3">
                  {(report.topProducts ?? []).slice(0, 8).map((p, i) => (
                    <div key={p.productId} className="flex items-center gap-3">
                      <span className="text-xs font-mono text-surface-400 w-5 flex-shrink-0">{i + 1}</span>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium truncate">{p.productName}</p>
                        <div className="flex items-center gap-2 mt-1">
                          <div className="flex-1 bg-surface-100 rounded-full h-1.5 overflow-hidden">
                            <div className="bg-emerald-500 h-full rounded-full"
                              style={{ width: `${(p.totalSold / Math.max(...(report.topProducts ?? [{}]).map(x => x.totalSold || 1))) * 100}%` }} />
                          </div>
                        </div>
                      </div>
                      <span className="text-sm font-semibold text-surface-700 flex-shrink-0">{p.totalSold} sold</span>
                    </div>
                  ))}
                  {(!report.topProducts || report.topProducts.length === 0) && (
                    <p className="text-surface-400 text-sm text-center py-8">No sales in this period</p>
                  )}
                </div>
              </div>
            </div>
          </>
        )}
      </div>
    </AdminLayout>
  )
}