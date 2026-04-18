import { useEffect, useState } from 'react'
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, BarChart, Bar, Cell } from 'recharts'
import { DollarSign, ShoppingBag, Users, AlertTriangle, TrendingUp, Package } from 'lucide-react'
import { adminApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { StatCard, StatusBadge, LoadingPage } from '../../components/ui'
import { format } from 'date-fns'

const STATUS_COLORS = {
  Pending: '#f59e0b', Confirmed: '#3b82f6', Processing: '#8b5cf6',
  Shipped: '#06b6d4', Delivered: '#10b981', Cancelled: '#ef4444'
}

export default function AdminDashboard() {
  const [stats, setStats] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    adminApi.dashboard().then(r => setStats(r.data)).finally(() => setLoading(false))
  }, [])

  if (loading) return <AdminLayout><LoadingPage /></AdminLayout>

  return (
    <AdminLayout>
      <div className="space-y-8 max-w-[1400px]">
        {/* Header */}
        <div>
          <h1 className="font-display text-2xl font-semibold text-surface-900">Dashboard</h1>
          <p className="text-surface-400 text-sm mt-0.5">
            {format(new Date(), "EEEE, MMMM d yyyy")}
          </p>
        </div>

        {/* KPI Cards */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard label="Total Revenue" value={`$${(stats.totalRevenue || 0).toLocaleString('en-CA', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}
            sub={`$${(stats.monthRevenue || 0).toFixed(2)} this month`} icon={DollarSign} color="brand" />
          <StatCard label="Total Orders" value={stats.totalOrders?.toLocaleString() ?? '0'}
            sub={`${stats.todayOrders} today`} icon={ShoppingBag} color="green" />
          <StatCard label="Customers" value={stats.totalCustomers?.toLocaleString() ?? '0'}
            sub={`+${stats.newCustomersThisMonth} this month`} icon={Users} color="amber" />
          <StatCard label="Low Stock Items" value={stats.lowStockProducts ?? 0}
            sub="Need restocking" icon={AlertTriangle} color={stats.lowStockProducts > 0 ? 'red' : 'green'} />
        </div>

        {/* Revenue Chart */}
        <div className="card p-6">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h2 className="font-display font-semibold text-lg">Revenue — Last 30 Days</h2>
              <p className="text-surface-400 text-sm">Daily revenue from paid orders</p>
            </div>
            <TrendingUp className="w-5 h-5 text-brand-500" />
          </div>
          <ResponsiveContainer width="100%" height={260}>
            <AreaChart data={stats.revenueChart ?? []} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
              <defs>
                <linearGradient id="revGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#2d52ff" stopOpacity={0.15} />
                  <stop offset="95%" stopColor="#2d52ff" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#f4f4f5" />
              <XAxis dataKey="date" tick={{ fontSize: 11, fill: '#a1a1aa' }}
                tickFormatter={d => d.slice(5)} tickLine={false} axisLine={false} />
              <YAxis tick={{ fontSize: 11, fill: '#a1a1aa' }} tickLine={false} axisLine={false}
                tickFormatter={v => `$${v}`} />
              <Tooltip contentStyle={{ borderRadius: 12, border: 'none', boxShadow: '0 4px 24px rgba(0,0,0,.12)', fontSize: 13 }}
                formatter={v => [`$${v.toFixed(2)}`, 'Revenue']} labelFormatter={l => `Date: ${l}`} />
              <Area type="monotone" dataKey="revenue" stroke="#2d52ff" strokeWidth={2}
                fill="url(#revGrad)" dot={false} activeDot={{ r: 4, strokeWidth: 0 }} />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* Bottom row */}
        <div className="grid lg:grid-cols-3 gap-6">
          {/* Orders by status */}
          <div className="card p-6">
            <h2 className="font-display font-semibold mb-5">Orders by Status</h2>
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={stats.ordersByStatus ?? []} layout="vertical" margin={{ left: 0, right: 16 }}>
                <XAxis type="number" tick={{ fontSize: 11, fill: '#a1a1aa' }} tickLine={false} axisLine={false} />
                <YAxis dataKey="status" type="category" tick={{ fontSize: 11, fill: '#71717a' }} width={80} tickLine={false} axisLine={false} />
                <Tooltip contentStyle={{ borderRadius: 10, border: 'none', fontSize: 12 }} />
                <Bar dataKey="count" radius={[0, 6, 6, 0]}>
                  {(stats.ordersByStatus ?? []).map((entry, i) => (
                    <Cell key={i} fill={STATUS_COLORS[entry.status] ?? '#a1a1aa'} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>

          {/* Top products */}
          <div className="card p-6 lg:col-span-2">
            <h2 className="font-display font-semibold mb-5">Top Selling Products</h2>
            <div className="space-y-3">
              {(stats.topProducts ?? []).slice(0, 5).map((p, i) => (
                <div key={p.productId} className="flex items-center gap-3">
                  <span className="w-6 text-xs font-mono text-surface-400 flex-shrink-0">{i + 1}</span>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-surface-900 truncate">{p.productName}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <div className="flex-1 bg-surface-100 rounded-full h-1.5 overflow-hidden">
                        <div className="bg-brand-500 h-full rounded-full transition-all"
                          style={{ width: `${(p.totalSold / (stats.topProducts[0]?.totalSold || 1)) * 100}%` }} />
                      </div>
                      <span className="text-xs text-surface-400 flex-shrink-0">{p.totalSold} sold</span>
                    </div>
                  </div>
                  <span className="text-sm font-semibold text-surface-900 flex-shrink-0">
                    ${p.revenue.toFixed(0)}
                  </span>
                </div>
              ))}
              {(!stats.topProducts || stats.topProducts.length === 0) && (
                <p className="text-surface-400 text-sm text-center py-6">No sales data yet</p>
              )}
            </div>
          </div>
        </div>
      </div>
    </AdminLayout>
  )
}
