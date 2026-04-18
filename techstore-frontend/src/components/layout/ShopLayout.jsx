import { Link, useNavigate, useLocation } from 'react-router-dom'
import { ShoppingCart, Search, Menu, X, User, LogOut, LayoutDashboard, Package } from 'lucide-react'
import { useState } from 'react'
import { clsx } from 'clsx'
import toast from 'react-hot-toast'
import { useAuthStore, useCartStore, useUIStore } from '../../store'
import { cartApi } from '../../services/api'
import { Spinner } from '../ui'

// ── Navbar ────────────────────────────────────────────────────────────────────
export function Navbar() {
  const { isAuthenticated, user, logout, isAdmin } = useAuthStore()
  const { itemCount } = useCartStore()
  const { toggleCart } = useUIStore()
  const navigate = useNavigate()
  const location = useLocation()
  const [menuOpen, setMenuOpen] = useState(false)
  const [userMenuOpen, setUserMenuOpen] = useState(false)

  const handleLogout = () => {
    logout()
    toast.success('Logged out')
    navigate('/')
    setUserMenuOpen(false)
  }

  const navLinks = [
    { to: '/products', label: 'All Products' },
    { to: '/products?categoryId=1', label: 'Phones' },
    { to: '/products?categoryId=2', label: 'Tablets' },
    { to: '/products?categoryId=3', label: 'Watches' },
  ]

  return (
    <header className="sticky top-0 z-40 bg-white/90 backdrop-blur-lg border-b border-surface-100">
      <nav className="page-container flex items-center justify-between h-16">
        {/* Logo */}
        <Link to="/" className="flex items-center gap-2 group">
          <div className="w-8 h-8 bg-brand-500 rounded-lg flex items-center justify-center">
            <Package className="w-4 h-4 text-white" />
          </div>
          <span className="font-display font-semibold text-lg text-surface-900">TechStore</span>
        </Link>

        {/* Desktop nav */}
        <div className="hidden md:flex items-center gap-1">
          {navLinks.map(l => (
            <Link key={l.to} to={l.to}
              className={clsx('px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                location.pathname + location.search === l.to
                  ? 'bg-brand-50 text-brand-700'
                  : 'text-surface-600 hover:text-surface-900 hover:bg-surface-50')}>
              {l.label}
            </Link>
          ))}
        </div>

        {/* Right actions */}
        <div className="flex items-center gap-2">
          <Link to="/products" className="btn-icon hidden sm:flex" aria-label="Search">
            <Search className="w-4 h-4" />
          </Link>

          {/* Cart */}
          <button onClick={toggleCart} className="btn-icon relative">
            <ShoppingCart className="w-4 h-4" />
            {itemCount > 0 && (
              <span className="absolute -top-0.5 -right-0.5 w-4 h-4 bg-brand-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center">
                {itemCount > 9 ? '9+' : itemCount}
              </span>
            )}
          </button>

          {/* User menu */}
          {isAuthenticated ? (
            <div className="relative">
              <button onClick={() => setUserMenuOpen(v => !v)}
                className="flex items-center gap-2 px-3 py-1.5 rounded-xl hover:bg-surface-100 transition-colors">
                <div className="w-7 h-7 rounded-full bg-brand-500 flex items-center justify-center text-white text-xs font-semibold">
                  {user?.firstName?.[0]}{user?.lastName?.[0]}
                </div>
                <span className="text-sm font-medium text-surface-700 hidden sm:block">{user?.firstName}</span>
              </button>
              {userMenuOpen && (
                <div className="absolute right-0 top-12 w-52 card py-2 shadow-lg z-50 animate-fade-in">
                  <Link to="/account/orders" className="flex items-center gap-2 px-4 py-2 text-sm text-surface-700 hover:bg-surface-50"
                    onClick={() => setUserMenuOpen(false)}>
                    <Package className="w-4 h-4" /> My Orders
                  </Link>
                  {isAdmin() && (
                    <Link to="/admin" className="flex items-center gap-2 px-4 py-2 text-sm text-brand-600 hover:bg-brand-50"
                      onClick={() => setUserMenuOpen(false)}>
                      <LayoutDashboard className="w-4 h-4" /> Admin Dashboard
                    </Link>
                  )}
                  <hr className="my-1 border-surface-100" />
                  <button onClick={handleLogout}
                    className="w-full flex items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50">
                    <LogOut className="w-4 h-4" /> Logout
                  </button>
                </div>
              )}
            </div>
          ) : (
            <Link to="/login" className="btn-primary btn-sm">Sign In</Link>
          )}

          {/* Mobile menu toggle */}
          <button onClick={() => setMenuOpen(v => !v)} className="btn-icon md:hidden">
            {menuOpen ? <X className="w-4 h-4" /> : <Menu className="w-4 h-4" />}
          </button>
        </div>
      </nav>

      {/* Mobile menu */}
      {menuOpen && (
        <div className="md:hidden border-t border-surface-100 bg-white py-3 px-4 animate-fade-in">
          {navLinks.map(l => (
            <Link key={l.to} to={l.to} onClick={() => setMenuOpen(false)}
              className="block py-2.5 text-sm text-surface-700 hover:text-brand-600">
              {l.label}
            </Link>
          ))}
        </div>
      )}
    </header>
  )
}

// ── Cart Drawer ────────────────────────────────────────────────────────────────
export function CartDrawer() {
  const { cartOpen, closeCart } = useUIStore()
  const { items, total, setCart } = useCartStore()
  const { isAuthenticated } = useAuthStore()
  const navigate = useNavigate()
  const [loadingItem, setLoadingItem] = useState(null)

  const handleRemove = async (itemId) => {
    setLoadingItem(itemId)
    try {
      const { data } = await cartApi.removeItem(itemId)
      setCart(data)
    } catch { toast.error('Could not remove item') }
    finally { setLoadingItem(null) }
  }

  const handleCheckout = () => {
    closeCart()
    if (!isAuthenticated) { navigate('/login'); return }
    navigate('/checkout')
  }

  return (
    <>
      {cartOpen && <div className="fixed inset-0 z-40 bg-black/30 backdrop-blur-sm" onClick={closeCart} />}
      <div className={clsx(
        'fixed top-0 right-0 z-50 h-full w-full max-w-sm bg-white shadow-2xl transition-transform duration-300 flex flex-col',
        cartOpen ? 'translate-x-0' : 'translate-x-full'
      )}>
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-surface-100">
          <h2 className="font-display font-semibold text-lg">Your Cart</h2>
          <button onClick={closeCart} className="btn-icon"><X className="w-4 h-4" /></button>
        </div>

        {/* Items */}
        <div className="flex-1 overflow-y-auto p-5 space-y-4">
          {items.length === 0 ? (
            <div className="text-center py-12">
              <ShoppingCart className="w-12 h-12 text-surface-200 mx-auto mb-3" />
              <p className="text-surface-400 text-sm">Your cart is empty</p>
              <button onClick={closeCart} className="btn-primary btn-sm mt-4">Continue Shopping</button>
            </div>
          ) : items.map(item => (
            <div key={item.id} className="flex gap-3">
              <div className="w-16 h-16 rounded-xl bg-surface-100 flex-shrink-0 overflow-hidden">
                {item.productImageUrl
                  ? <img src={item.productImageUrl} alt={item.productName} className="w-full h-full object-cover" />
                  : <div className="w-full h-full flex items-center justify-center text-surface-300"><Package className="w-6 h-6" /></div>}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-surface-900 truncate">{item.productName}</p>
                <p className="text-xs text-surface-400">${item.unitPrice.toFixed(2)} × {item.quantity}</p>
                <p className="text-sm font-semibold text-brand-600">${item.lineTotal.toFixed(2)}</p>
              </div>
              <button onClick={() => handleRemove(item.id)} disabled={loadingItem === item.id}
                className="btn-icon text-surface-400 hover:text-red-500 self-start">
                {loadingItem === item.id ? <Spinner size="sm" /> : <X className="w-3.5 h-3.5" />}
              </button>
            </div>
          ))}
        </div>

        {/* Footer */}
        {items.length > 0 && (
          <div className="p-5 border-t border-surface-100 space-y-3">
            <div className="flex justify-between text-sm">
              <span className="text-surface-500">Subtotal</span>
              <span className="font-semibold">${total.toFixed(2)}</span>
            </div>
            <p className="text-xs text-surface-400">Tax + shipping calculated at checkout</p>
            <button onClick={handleCheckout} className="btn-primary w-full btn-lg">
              Checkout →
            </button>
          </div>
        )}
      </div>
    </>
  )
}

// ── Footer ─────────────────────────────────────────────────────────────────────
export function Footer() {
  return (
    <footer className="bg-surface-950 text-surface-400 py-16 mt-24">
      <div className="page-container">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-12">
          <div className="col-span-2 md:col-span-1">
            <div className="flex items-center gap-2 mb-4">
              <div className="w-7 h-7 bg-brand-500 rounded-lg flex items-center justify-center">
                <Package className="w-3.5 h-3.5 text-white" />
              </div>
              <span className="font-display font-semibold text-white">TechStore</span>
            </div>
            <p className="text-sm leading-relaxed">Premium tech, delivered fast. Canada's favourite online electronics retailer.</p>
          </div>
          {[
            { heading: 'Shop', links: ['Smartphones', 'Tablets', 'Smart Watches', 'Laptops', 'Accessories'] },
            { heading: 'Support', links: ['Track Order', 'Returns', 'Warranty', 'Contact Us'] },
            { heading: 'Company', links: ['About', 'Careers', 'Press', 'Privacy Policy'] },
          ].map(col => (
            <div key={col.heading}>
              <h4 className="text-white font-medium text-sm mb-4">{col.heading}</h4>
              <ul className="space-y-2">
                {col.links.map(l => (
                  <li key={l}><a href="#" className="text-sm hover:text-white transition-colors">{l}</a></li>
                ))}
              </ul>
            </div>
          ))}
        </div>
        <div className="border-t border-surface-800 pt-8 text-xs text-center">
          © {new Date().getFullYear()} TechStore Inc. All rights reserved.
        </div>
      </div>
    </footer>
  )
}
