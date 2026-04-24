import { Link, useLocation, useNavigate } from 'react-router-dom'
import { clsx } from 'clsx'
import {
  LayoutDashboard, Package, ShoppingBag, Users,
  BarChart3, Warehouse, LogOut, Menu, X, Package2, Bell, ChevronRight, Image
} from 'lucide-react'
import { useState } from 'react'
import { useAuthStore } from '../../store'
import { authApi } from '../../services/api'
import toast from 'react-hot-toast'

const navSections = [
  {
    label: 'Overview',
    items: [
      { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, exact: true },
      { to: '/admin/reports', label: 'Reports', icon: BarChart3 },
    ]
  },
  {
    label: 'Catalog',
    items: [
      { to: '/admin/products',  label: 'Products',  icon: Package },
      { to: '/admin/inventory', label: 'Inventory', icon: Warehouse },
      { to: '/admin/images',    label: 'Images',    icon: Image },
    ]
  },
  {
    label: 'Sales',
    items: [
      { to: '/admin/orders', label: 'Orders', icon: ShoppingBag },
      { to: '/admin/users', label: 'Customers', icon: Users },
    ]
  },
]

function NavItem({ item, onClick }) {
  const location = useLocation()
  const active = item.exact
    ? location.pathname === item.to
    : location.pathname.startsWith(item.to)

  return (
    <Link to={item.to} onClick={onClick}
      className={clsx('nav-item', active && 'active')}>
      <item.icon className="w-4 h-4 flex-shrink-0" />
      {item.label}
    </Link>
  )
}

export function AdminLayout({ children }) {
  const { user, logout } = useAuthStore()
  const navigate = useNavigate()
  const [sidebarOpen, setSidebarOpen] = useState(false)

  const handleLogout = async () => {
    try { await authApi.logout() } catch { /* cookie cleared on client regardless */ }
    logout()
    toast.success('Logged out')
    navigate('/login')
  }

  const Sidebar = ({ onClose }) => (
    <aside className="flex flex-col h-full w-64 bg-white border-r border-surface-100">
      {/* Brand */}
      <div className="flex items-center justify-between p-5 border-b border-surface-100">
        <Link to="/" className="flex items-center gap-3">
          <img
            src="/hytel-logo.png"
            alt="Hytel Phones"
            className="h-9 w-auto object-contain"
          />
          <div>
            <p className="font-display font-semibold text-sm leading-none">Hytel Phones</p>
            <p className="text-[10px] text-surface-400 mt-0.5">Admin Panel</p>
          </div>
        </Link>
        {onClose && (
          <button onClick={onClose} className="btn-icon lg:hidden"><X className="w-4 h-4" /></button>
        )}
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto p-3 space-y-5">
        {navSections.map(section => (
          <div key={section.label}>
            <p className="text-[10px] font-semibold text-surface-400 uppercase tracking-widest px-3 mb-1">
              {section.label}
            </p>
            <div className="space-y-0.5">
              {section.items.map(item => (
                <NavItem key={item.to} item={item} onClick={onClose} />
              ))}
            </div>
          </div>
        ))}
      </nav>

      {/* View store link */}
      <div className="p-3 border-t border-surface-100">
        <Link to="/" className="nav-item text-xs text-surface-400 mb-1">
          <ChevronRight className="w-3.5 h-3.5" /> View Storefront
        </Link>
      </div>

      {/* User */}
      <div className="p-3 border-t border-surface-100">
        <div className="flex items-center gap-3 px-2 py-2">
          <div className="w-8 h-8 rounded-full bg-brand-500 flex items-center justify-center text-white text-xs font-semibold flex-shrink-0">
            {user?.firstName?.[0]}{user?.lastName?.[0]}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-surface-900 truncate">{user?.firstName} {user?.lastName}</p>
            <p className="text-xs text-surface-400 truncate">{user?.email}</p>
          </div>
          <button onClick={handleLogout} className="btn-icon text-surface-400 hover:text-red-500" title="Logout">
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </div>
    </aside>
  )

  return (
    <div className="flex h-screen bg-surface-50 overflow-hidden">
      {/* Desktop sidebar */}
      <div className="hidden lg:flex flex-shrink-0">
        <Sidebar />
      </div>

      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-50 flex lg:hidden">
          <div className="absolute inset-0 bg-black/30" onClick={() => setSidebarOpen(false)} />
          <div className="relative z-10 animate-slide-in">
            <Sidebar onClose={() => setSidebarOpen(false)} />
          </div>
        </div>
      )}

      {/* Main content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top bar */}
        <header className="bg-white border-b border-surface-100 flex items-center justify-between px-6 h-14 flex-shrink-0">
          <button onClick={() => setSidebarOpen(true)} className="btn-icon lg:hidden">
            <Menu className="w-4 h-4" />
          </button>
          <div className="flex items-center gap-2 ml-auto">
            <button className="btn-icon relative">
              <Bell className="w-4 h-4" />
              <span className="absolute top-1 right-1 w-1.5 h-1.5 bg-red-500 rounded-full" />
            </button>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-3 sm:p-6">
          {children}
        </main>
      </div>
    </div>
  )
}