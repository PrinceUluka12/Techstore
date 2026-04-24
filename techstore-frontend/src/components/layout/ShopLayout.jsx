import { Link, useNavigate, useLocation } from 'react-router-dom'
import { ShoppingCart, Search, Menu, X, User, LogOut, LayoutDashboard, Package } from 'lucide-react'
import { useState } from 'react'
import { clsx } from 'clsx'
import toast from 'react-hot-toast'
import { useAuthStore, useCartStore, useUIStore } from '../../store'
import { authApi, cartApi } from '../../services/api'
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

  const handleLogout = async () => {
    try { await authApi.logout() } catch { /* cookie cleared on client regardless */ }
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
        {/* Logo — Hytel icon only, no text */}
        <Link to="/" className="flex items-center group">
          <img
            src="/hytel-logo.png"
            alt="Hytel Phones"
            className="h-10 w-auto object-contain group-hover:opacity-85 transition-opacity"
          />
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
              <div className="w-16 h-16 rounded-xl bg-white border border-surface-100 flex-shrink-0 overflow-hidden">
                {item.productImageUrl
                  ? <img src={item.productImageUrl} alt={item.productName} className="w-full h-full object-contain p-1" />
                  : <div className="w-full h-full flex items-center justify-center text-surface-300"><Package className="w-6 h-6" /></div>}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-surface-900 truncate">{item.productName}</p>
                <p className="text-xs text-surface-400">₦{item.unitPrice.toFixed(2)} × {item.quantity}</p>
                <p className="text-sm font-semibold text-brand-600">₦{item.lineTotal.toFixed(2)}</p>
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
              <span className="font-semibold">₦{total.toFixed(2)}</span>
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
    <footer className="bg-[#1e1e1e] text-surface-400 pt-14 pb-8 mt-16">
      <div className="page-container">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-10 mb-10">

          {/* Col 1 — Get in touch */}
          <div>
            <h4 className="text-white font-bold text-sm mb-5">Get in touch</h4>
            <ul className="space-y-3 text-sm leading-relaxed">
              <li>
                <span className="text-white font-semibold">Phone:</span>{' '}
                8am to 6pm 08034888246 (Mon-Sat)
              </li>
              <li>
                <span className="text-white font-semibold">Email:</span>{' '}
                <a href="mailto:support@hytelphones.ng"
                  className="hover:text-white transition-colors">
                  support@hytelphones.ng
                </a>
              </li>
              <li>
                <span className="text-white font-semibold">Address:</span>{' '}
                Head Office No 30A Wethdral Road by Cherubim Junction Owerri, Imo State.
              </li>
              <li className="pt-1">
                <span className="text-white font-semibold">Nokia House:</span>{' '}
                107/108 Wethdral Rd. opposite Baptist High School Owerri, Imo State
              </li>
            </ul>
          </div>

          {/* Col 2 — Useful links */}
          <div>
            <h4 className="text-white font-bold text-sm mb-5">Useful links</h4>
            <ul className="space-y-3">
              {[
                { label: 'My account',                to: '/account/orders' },
                { label: 'Shop',                      to: '/products' },
                { label: 'Terms of Service & Compliance', to: '#' },
                { label: 'Refund and Returns Policy', to: '#' },
                { label: 'Disclaimer',                to: '#' },
                { label: 'Privacy Policy',            to: '#' },
                { label: 'Help & Support',            to: '#' },
              ].map(l => (
                <li key={l.label}>
                  <Link to={l.to}
                    className="text-sm hover:text-white transition-colors">
                    {l.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Col 3 — Other Service Centres */}
          <div>
            <div className="border-l-2 border-surface-600 pl-4">
              <h4 className="text-white font-bold text-sm mb-5">Other Services Centres</h4>
              <ul className="space-y-5 text-sm">
                <li>
                  <p>
                    <span className="text-white font-bold">Hytel Phones Orlu:</span>{' '}
                    No 3 Ihioma road by Chisco Park Orlu, Imo State.
                  </p>
                </li>
                <li>
                  <p>
                    <span className="text-white font-bold">Hytel Phones Anambra State</span>{' '}
                    University Rd. Uli, Anambra State
                  </p>
                </li>
                <li>
                  <p>
                    <span className="text-white font-bold">Hytel Phones Lagos:</span>{' '}
                    23 Broad Street, Lagos Island, Lagos State.
                  </p>
                </li>
              </ul>
            </div>
          </div>

          {/* Col 4 — Newsletter */}
          <div>
            <h4 className="text-white font-bold text-sm mb-5">Let's stay in touch</h4>
            <div className="flex gap-0 mb-4">
              <input
                type="email"
                placeholder="Your email address"
                className="flex-1 bg-white text-surface-900 text-sm px-4 py-2.5 rounded-l-lg focus:outline-none placeholder:text-surface-400 min-w-0"
              />
              <button className="bg-surface-800 hover:bg-surface-700 text-white text-sm font-semibold px-4 py-2.5 rounded-r-lg border border-surface-600 hover:border-surface-500 transition-colors flex-shrink-0">
                Subscribe
              </button>
            </div>
            <p className="text-sm leading-relaxed">
              Keep up to date with our latest news and special offers.
            </p>

            {/* Social icons */}
            <div className="flex items-center gap-3 mt-5">
              {[
                { label: 'Facebook', path: 'M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z' },
                { label: 'Twitter', path: 'M23 3a10.9 10.9 0 0 1-3.14 1.53A4.48 4.48 0 0 0 22.43.36a9 9 0 0 1-2.88 1.1A4.52 4.52 0 0 0 16.11 0c-2.5 0-4.52 2.02-4.52 4.52 0 .35.04.7.11 1.03C7.69 5.37 4.07 3.58 1.64.9a4.52 4.52 0 0 0-.61 2.27c0 1.57.8 2.95 2.01 3.76a4.49 4.49 0 0 1-2.05-.57v.06c0 2.19 1.56 4.02 3.63 4.43-.38.1-.78.16-1.19.16-.29 0-.57-.03-.85-.08.57 1.79 2.24 3.09 4.21 3.12A9.06 9.06 0 0 1 0 19.54a12.8 12.8 0 0 0 6.92 2.03c8.3 0 12.85-6.88 12.85-12.85 0-.2 0-.39-.01-.58A9.17 9.17 0 0 0 23 3z' },
                { label: 'Instagram', path: 'M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37zM17.5 6.5h.01M7.5 2h9A5.5 5.5 0 0 1 22 7.5v9A5.5 5.5 0 0 1 16.5 22h-9A5.5 5.5 0 0 1 2 16.5v-9A5.5 5.5 0 0 1 7.5 2z' },
              ].map(s => (
                <a key={s.label} href="#" aria-label={s.label}
                  className="w-8 h-8 rounded-full bg-surface-700 hover:bg-brand-500 flex items-center justify-center transition-colors">
                  <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4 text-white" fill="none"
                    viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d={s.path} />
                  </svg>
                </a>
              ))}
            </div>
          </div>
        </div>

        {/* Bottom bar */}
        <div className="border-t border-surface-800 pt-6 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
          <span>© {new Date().getFullYear()} Hytel Phones Nigeria. All rights reserved.</span>
          <div className="flex items-center gap-2">
            {/* Payment icons */}
            {['Visa', 'Mastercard', 'Verve'].map(p => (
              <span key={p}
                className="px-2.5 py-1 bg-surface-800 border border-surface-700 rounded text-[10px] font-semibold text-surface-300">
                {p}
              </span>
            ))}
          </div>
        </div>
      </div>
    </footer>
  )
}