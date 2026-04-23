import { useEffect, useState } from 'react'
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, BarChart, Bar, Cell,
} from 'recharts'
import {
  DollarSign, ShoppingBag, Users, AlertTriangle,
  TrendingUp, Package, RefreshCw, Clock,
} from 'lucide-react'
import { adminApi, orderApi, inventoryApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { StatCard, StatusBadge, LoadingPage, Spinner } from '../../components/ui'
import { format } from 'date-fns'
import { Link } from 'react-router-dom'

const STATUS_COLORS = {
  Pending: '#f59e0b', Confirmed: '#3b82f6', Processing: '#8b5cf6',
  Shipped: '#06b6d4', Delivered: '#10b981', Cancelled: '#ef4444', Refunded: '#a1a1aa',
}

const EMPTY_STATS = {
  totalRevenue: 0, monthRevenue: 0,
  totalOrders: 0, todayOrders: 0,
  totalCustomers: 0, newCustomersThisMonth: 0,
  totalProducts: 0, activeProducts: 0, lowStockProducts: 0,
  revenueChart: [], topProducts: [], ordersByStatus: [],
}

function RecentOrdersTable({ orders }) {
  if (!orders.length) return (
    <p className="text-surface-400 text-sm text-center py-8">No orders yet</p>
  )
  return (
    <div>
      {orders.map((o, i) => (
        <div key={o.id} className={`flex items-center gap-3 py-3 ${i > 0 ? 'border-t border-surface-50' : ''}`}>
          <div className="w-8 h-8 rounded-lg bg-brand-50 flex items-center justify-center flex-shrink-0">
            <ShoppingBag className="w-3.5 h-3.5 text-brand-500" />
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-mono font-medium text-surface-900 truncate">{o.orderNumber}</p>
            <p className="text-xs text-surface-400 truncate">{o.customerName}</p>
          </div>
          <div className="text-right flex-shrink-0 space-y-0.5">
            <p className="text-sm font-semibold text-surface-900">₦{Number(o.total).toFixed(2)}</p>
            <StatusBadge status={o.status} />
          </div>
        </div>
      ))}
    </div>
  )
}

function LowStockTable({ items }) {
  if (!items.length) return (
    <p className="text-emerald-600 text-sm text-center py-8">All products well stocked ✓</p>
  )
  return (
    <div>
      {items.slice(0, 6).map((inv, i) => (
        <div key={inv.id} className={`flex items-center gap-3 py-3 ${i > 0 ? 'border-t border-surface-50' : ''}`}>
          <div className={`w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 ${inv.quantityAvailable <= 0 ? 'bg-red-50' : 'bg-amber-50'}`}>
            <Package className={`w-3.5 h-3.5 ${inv.quantityAvailable <= 0 ? 'text-red-500' : 'text-amber-500'}`} />
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-surface-900 truncate">{inv.productName}</p>
            <p className="text-xs font-mono text-surface-400">{inv.productSKU}</p>
          </div>
          <div className="text-right flex-shrink-0">
            <p className={`text-sm font-bold ${inv.quantityAvailable <= 0 ? 'text-red-500' : 'text-amber-600'}`}>
              {inv.quantityAvailable}
            </p>
            <p className="text-xs text-surface-400">in stock</p>
          </div>
        </div>
      ))}
    </div>
  )
}

export default function AdminDashboard() {
  const [stats, setStats] = useState(null)
  const [recentOrders, setRecentOrders] = useState([])
  const [lowStock, setLowStock] = useState([])
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState(null)

  const load = async (isRefresh = false) => {
    if (isRefresh) setRefreshing(true)
    else setLoading(true)
    setError(null)

    try {
      const [statsRes, ordersRes, stockRes] = await Promise.allSettled([
        adminApi.dashboard(),
        orderApi.getAll({ page: 1, pageSize: 6 }),
        inventoryApi.lowStock(),
      ])

      if (statsRes.status === 'fulfilled' && statsRes.value?.data) {
        setStats({ ...EMPTY_STATS, ...statsRes.value.data })
      } else {
        setError('Could not load dashboard stats. Is the API running?')
        setStats(EMPTY_STATS)
      }

      if (ordersRes.status === 'fulfilled') {
        const d = ordersRes.value?.data
        setRecentOrders(Array.isArray(d) ? d : (d?.items ?? []))
      }

      if (stockRes.status === 'fulfilled') {
        setLowStock(Array.isArray(stockRes.value?.data) ? stockRes.value.data : [])
      }
    } catch (err) {
      setError('Could not connect to the server. Check the API is running.')
      setStats(EMPTY_STATS)
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  }

  useEffect(() => { load() }, [])

  if (loading) return <AdminLayout><LoadingPage /></AdminLayout>

  // Hard error — API completely unreachable
  if (error && !stats) return (
    <AdminLayout>
      <div className="flex flex-col items-center justify-center h-[60vh] gap-4">
        <div className="w-14 h-14 bg-red-50 rounded-2xl flex items-center justify-center">
          <AlertTriangle className="w-7 h-7 text-red-500" />
        </div>
        <div className="text-center">
          <p className="font-semibold text-surface-900">{error}</p>
          <p className="text-surface-400 text-sm mt-1">Make sure the backend is running on the correct port</p>
        </div>
        <button onClick={() => load()} className="btn-primary">Try Again</button>
      </div>
    </AdminLayout>
  )

  const s = stats || EMPTY_STATS

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="font-display text-2xl font-semibold text-surface-900">Dashboard</h1>
            <p className="text-surface-400 text-sm mt-0.5 flex items-center gap-1.5">
              <Clock className="w-3.5 h-3.5" />
              {format(new Date(), "EEEE, MMMM d yyyy · h:mm a")}
            </p>
          </div>
          <button onClick={() => load(true)} disabled={refreshing} className="btn-secondary gap-2">
            {refreshing ? <Spinner size="sm" /> : <RefreshCw className="w-4 h-4" />}
            Refresh
          </button>
        </div>

        {/* Soft error banner */}
        {error && (
          <div className="bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 text-sm text-amber-700 flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 flex-shrink-0" />
            {error}
          </div>
        )}

        {/* KPI Cards */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <StatCard
            label="Total Revenue"
            value={`₦${Number(s.totalRevenue).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}
            sub={`₦${Number(s.monthRevenue).toFixed(2)} this month`}
            icon={DollarSign} color="brand"
          />
          <StatCard
            label="Total Orders"
            value={Number(s.totalOrders).toLocaleString()}
            sub={`${s.todayOrders} today`}
            icon={ShoppingBag} color="green"
          />
          <StatCard
            label="Customers"
            value={Number(s.totalCustomers).toLocaleString()}
            sub={`+${s.newCustomersThisMonth} this month`}
            icon={Users} color="amber"
          />
          <StatCard
            label="Low Stock Items"
            value={s.lowStockProducts ?? 0}
            sub="Need restocking"
            icon={AlertTriangle}
            color={s.lowStockProducts > 0 ? 'red' : 'green'}
          />
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
          {s.revenueChart.length === 0 ? (
            <div className="h-[260px] flex items-center justify-center">
              <p className="text-surface-400 text-sm">No revenue data yet</p>
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={260}>
              <AreaChart data={s.revenueChart} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
                <defs>
                  <linearGradient id="revGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%"  stopColor="#2d52ff" stopOpacity={0.15} />
                    <stop offset="95%" stopColor="#2d52ff" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#f4f4f5" />
                <XAxis dataKey="date" tick={{ fontSize: 11, fill: '#a1a1aa' }}
                  tickFormatter={d => d.slice(5)} tickLine={false} axisLine={false} />
                <YAxis tick={{ fontSize: 11, fill: '#a1a1aa' }} tickLine={false} axisLine={false}
                  tickFormatter={v => `₦${v}`} />
                <Tooltip
                  contentStyle={{ borderRadius: 12, border: 'none', boxShadow: '0 4px 24px rgba(0,0,0,.12)', fontSize: 13 }}
                  formatter={v => [`₦${Number(v).toFixed(2)}`, 'Revenue']}
                  labelFormatter={l => `Date: ${l}`}
                />
                <Area type="monotone" dataKey="revenue" stroke="#2d52ff" strokeWidth={2}
                  fill="url(#revGrad)" dot={false} activeDot={{ r: 4, strokeWidth: 0 }} />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </div>

        {/* Middle row */}
        <div className="grid lg:grid-cols-3 gap-6">
          {/* Orders by status */}
          <div className="card p-6">
            <h2 className="font-display font-semibold mb-5">Orders by Status</h2>
            {s.ordersByStatus.length === 0 ? (
              <p className="text-surface-400 text-sm text-center py-8">No orders yet</p>
            ) : (
              <ResponsiveContainer width="100%" height={200}>
                <BarChart data={s.ordersByStatus} layout="vertical" margin={{ left: 0, right: 16 }}>
                  <XAxis type="number" tick={{ fontSize: 11, fill: '#a1a1aa' }}
                    tickLine={false} axisLine={false} allowDecimals={false} />
                  <YAxis dataKey="status" type="category" tick={{ fontSize: 11, fill: '#71717a' }}
                    width={80} tickLine={false} axisLine={false} />
                  <Tooltip contentStyle={{ borderRadius: 10, border: 'none', fontSize: 12 }}
                    formatter={v => [v, 'Orders']} />
                  <Bar dataKey="count" radius={[0, 6, 6, 0]}>
                    {s.ordersByStatus.map((entry, i) => (
                      <Cell key={i} fill={STATUS_COLORS[entry.status] ?? '#a1a1aa'} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>

          {/* Top products */}
          <div className="card p-6 lg:col-span-2">
            <div className="flex items-center justify-between mb-5">
              <h2 className="font-display font-semibold">Top Selling Products</h2>
              <Link to="/admin/products" className="text-xs text-brand-500 hover:text-brand-600">View all →</Link>
            </div>
            {s.topProducts.length === 0 ? (
              <p className="text-surface-400 text-sm text-center py-8">No sales data yet</p>
            ) : (
              <div className="space-y-3">
                {s.topProducts.slice(0, 5).map((p, i) => (
                  <div key={p.productId} className="flex items-center gap-3">
                    <span className="w-5 text-xs font-mono text-surface-400 flex-shrink-0">{i + 1}</span>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-surface-900 truncate">{p.productName}</p>
                      <div className="flex items-center gap-2 mt-1">
                        <div className="flex-1 bg-surface-100 rounded-full h-1.5 overflow-hidden">
                          <div className="bg-brand-500 h-full rounded-full transition-all duration-500"
                            style={{ width: `${(p.totalSold / (s.topProducts[0]?.totalSold || 1)) * 100}%` }} />
                        </div>
                        <span className="text-xs text-surface-400 flex-shrink-0">{p.totalSold} sold</span>
                      </div>
                    </div>
                    <span className="text-sm font-semibold text-surface-900 flex-shrink-0">
                      ${Number(p.revenue).toFixed(0)}
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Bottom row */}
        <div className="grid lg:grid-cols-2 gap-6">
          <div className="card p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-display font-semibold">Recent Orders</h2>
              <Link to="/admin/orders" className="text-xs text-brand-500 hover:text-brand-600">View all →</Link>
            </div>
            <RecentOrdersTable orders={recentOrders} />
          </div>

          <div className="card p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-display font-semibold">Low Stock Alerts</h2>
              <Link to="/admin/inventory" className="text-xs text-brand-500 hover:text-brand-600">Manage →</Link>
            </div>
            <LowStockTable items={lowStock} />
          </div>
        </div>

      </div>
    </AdminLayout>
  )
}