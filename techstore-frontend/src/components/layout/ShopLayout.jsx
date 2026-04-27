import { Link, useNavigate, useLocation } from 'react-router-dom'
import {
  ShoppingCart, Search, Menu, X, User, LogOut, LayoutDashboard, Package,
  ShoppingBag, Warehouse, Users, BarChart3, Tag, Image, Heart
} from 'lucide-react'
import { useState } from 'react'
import { clsx } from 'clsx'
import toast from 'react-hot-toast'
import { useAuthStore, useCartStore, useUIStore } from '../../store'
import { authApi, cartApi } from '../../services/api'
import { Spinner } from '../ui'

// Pages a staff member can access, in sidebar order
const STAFF_PAGES = [
  { to: '/admin',           label: 'Dashboard',  icon: LayoutDashboard, adminOnly: true },
  { to: '/admin/orders',    label: 'Orders',      icon: ShoppingBag,    perms: ['orders.view', 'orders.manage'] },
  { to: '/admin/products',  label: 'Products',    icon: Package,        perms: ['products.manage'] },
  { to: '/admin/inventory', label: 'Inventory',   icon: Warehouse,      perms: ['inventory.manage'] },
  { to: '/admin/users',     label: 'Customers',   icon: Users,          perms: ['users.view', 'users.manage'] },
  { to: '/admin/reports',   label: 'Reports',     icon: BarChart3,      perms: ['reports.view'] },
  { to: '/admin/coupons',   label: 'Coupons',     icon: Tag,            perms: ['coupons.manage'] },
  { to: '/admin/images',    label: 'Images',      icon: Image,          perms: ['images.manage'] },
]

// ── Navbar ────────────────────────────────────────────────────────────────────
export function Navbar() {
  const { isAuthenticated, user, logout, isAdmin, hasPermission, hasAnyAdminPermission } = useAuthStore()
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
                  {/* Name badge */}
                  <div className="px-4 py-2 border-b border-surface-100 mb-1">
                    <p className="text-xs font-semibold text-surface-900 truncate">{user?.firstName} {user?.lastName}</p>
                    <p className="text-[11px] text-surface-400 truncate">{user?.role}</p>
                  </div>

                  {/* Customer links */}
                  {user?.role === 'Customer' && (
                    <>
                      <Link to="/account/orders" className="flex items-center gap-2 px-4 py-2 text-sm text-surface-700 hover:bg-surface-50" onClick={() => setUserMenuOpen(false)}>
                        <Package className="w-4 h-4" /> My Orders
                      </Link>
                      <Link to="/account/profile" className="flex items-center gap-2 px-4 py-2 text-sm text-surface-700 hover:bg-surface-50" onClick={() => setUserMenuOpen(false)}>
                        <User className="w-4 h-4" /> My Profile
                      </Link>
                      <Link to="/account/wishlist" className="flex items-center gap-2 px-4 py-2 text-sm text-surface-700 hover:bg-surface-50" onClick={() => setUserMenuOpen(false)}>
                        <Heart className="w-4 h-4" /> Wishlist
                      </Link>
                    </>
                  )}

                  {/* Admin links */}
                  {isAdmin() && (
                    <Link to="/admin" className="flex items-center gap-2 px-4 py-2 text-sm text-brand-600 hover:bg-brand-50" onClick={() => setUserMenuOpen(false)}>
                      <LayoutDashboard className="w-4 h-4" /> Admin Dashboard
                    </Link>
                  )}

                  {/* Custom-role staff links */}
                  {!isAdmin() && hasAnyAdminPermission() && STAFF_PAGES
                    .filter(p => !p.adminOnly && p.perms?.some(perm => hasPermission(perm)))
                    .map(p => (
                      <Link key={p.to} to={p.to} className="flex items-center gap-2 px-4 py-2 text-sm text-brand-600 hover:bg-brand-50" onClick={() => setUserMenuOpen(false)}>
                        <p.icon className="w-4 h-4" /> {p.label}
                      </Link>
                    ))
                  }

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
        <div className="md:hidden border-t border-surface-100 bg-white py-3 px-4 animate-fade-in space-y-1">
          <div className="relative mb-2">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400 pointer-events-none" />
            <input
              className="input pl-9 text-sm"
              placeholder="Search products…"
              onKeyDown={e => {
                if (e.key === 'Enter' && e.target.value.trim()) {
                  navigate(`/products?query=${encodeURIComponent(e.target.value.trim())}`)
                  setMenuOpen(false)
                }
              }}
            />
          </div>
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
                <p className="text-xs text-surface-400">₦{item.unitPrice.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} × {item.quantity}</p>
                <p className="text-sm font-semibold text-brand-600">₦{item.lineTotal.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
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
              <span className="font-semibold">₦{total.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
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
              <a href="https://www.facebook.com/hytelfanclub" target="_blank" rel="noopener noreferrer"
                aria-label="Facebook"
                className="w-8 h-8 rounded-full bg-surface-700 hover:bg-[#1877f2] flex items-center justify-center transition-colors">
                <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z" />
                </svg>
              </a>
              <a href="https://www.instagram.com/hytelphones/" target="_blank" rel="noopener noreferrer"
                aria-label="Instagram"
                className="w-8 h-8 rounded-full bg-surface-700 hover:bg-[#e1306c] flex items-center justify-center transition-colors">
                <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37zM17.5 6.5h.01M7.5 2h9A5.5 5.5 0 0 1 22 7.5v9A5.5 5.5 0 0 1 16.5 22h-9A5.5 5.5 0 0 1 2 16.5v-9A5.5 5.5 0 0 1 7.5 2z" />
                </svg>
              </a>
              <a href="https://api.whatsapp.com/send/?phone=2348033360284&text&type=phone_number&app_absent=0" target="_blank" rel="noopener noreferrer"
                aria-label="WhatsApp"
                className="w-8 h-8 rounded-full bg-surface-700 hover:bg-[#25d366] flex items-center justify-center transition-colors">
                <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 0 1-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 0 1-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 0 1 2.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0 0 12.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 0 0 5.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 0 0-3.48-8.413z"/>
                </svg>
              </a>
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