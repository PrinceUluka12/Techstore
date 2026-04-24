import { useEffect, useState } from 'react'
import { AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import { UserCheck, UserX, TrendingUp, Calendar } from 'lucide-react'
import { adminApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Spinner, Pagination, StatusBadge } from '../../components/ui'
import toast from 'react-hot-toast'
import { format, subDays } from 'date-fns'

// ── Users Page ────────────────────────────────────────────────────────────────
export function AdminUsers() {
  const [users, setUsers] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const pageSize = 20
  const totalPages = Math.ceil(total / pageSize)

  const load = () => {
    setLoading(true)
    adminApi.getUsers({ page, pageSize })
      .then(r => { setUsers(r.data.items); setTotal(r.data.total) })
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [page])

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
      <div className="space-y-6 max-w-[1400px]">
        <div>
          <h1 className="font-display text-2xl font-semibold">Customers</h1>
          <p className="text-surface-400 text-sm mt-0.5">{total} registered customers</p>
        </div>

        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Customer</th>
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
                      <div className="w-8 h-8 rounded-full bg-brand-500 flex items-center justify-center text-white text-xs font-semibold flex-shrink-0">
                        {u.firstName?.[0]}{u.lastName?.[0]}
                      </div>
                      <span className="font-medium text-surface-900">{u.firstName} {u.lastName}</span>
                    </div>
                  </td>
                  <td><span className="text-sm text-surface-600">{u.email}</span></td>
                  <td><span className="text-sm text-surface-500">{u.phone || '—'}</span></td>
                  <td>
                    <span className={`badge ${u.role === 'Admin' ? 'badge-blue' : 'badge-gray'}`}>{u.role}</span>
                  </td>
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