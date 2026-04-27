import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { HelmetProvider } from 'react-helmet-async'
import { useAuthStore } from './store'
import ErrorBoundary from './components/ErrorBoundary'

// Shop pages
import HomePage from './pages/shop/HomePage'
import ProductsPage from './pages/shop/ProductsPage'
import ProductDetailPage from './pages/shop/ProductDetailPage'
import { LoginPage, RegisterPage, CheckoutPage } from './pages/shop/AuthPages'
import { MyOrdersPage, OrderDetailPage } from './pages/shop/OrdersPage'
import ForgotPasswordPage from './pages/shop/ForgotPasswordPage'
import ResetPasswordPage from './pages/shop/ResetPasswordPage'
import AccountProfilePage from './pages/shop/AccountProfilePage'
import WishlistPage from './pages/shop/WishlistPage'

// Admin pages
import AdminDashboard from './pages/admin/AdminDashboard'
import AdminProducts from './pages/admin/AdminProducts'
import AdminOrders from './pages/admin/AdminOrders'
import AdminInventory from './pages/admin/AdminInventory'
import { AdminUsers, AdminReports } from './pages/admin/AdminUsersReports'
import AdminImages from './pages/admin/AdminImages'
import AdminCoupons from './pages/admin/AdminCoupons'
import AdminRoles from './pages/admin/AdminRoles'

// ── Route guards ──────────────────────────────────────────────────────────────
function RequireAuth({ children }) {
  const { isAuthenticated } = useAuthStore()
  return isAuthenticated ? children : <Navigate to="/login" replace />
}

const PERMISSION_TO_ROUTE = [
  { perm: 'orders.view',      route: '/admin/orders' },
  { perm: 'orders.manage',    route: '/admin/orders' },
  { perm: 'products.manage',  route: '/admin/products' },
  { perm: 'inventory.manage', route: '/admin/inventory' },
  { perm: 'users.view',       route: '/admin/users' },
  { perm: 'users.manage',     route: '/admin/users' },
  { perm: 'reports.view',     route: '/admin/reports' },
  { perm: 'coupons.manage',   route: '/admin/coupons' },
  { perm: 'images.manage',    route: '/admin/images' },
]

function RequireAdmin({ children }) {
  const { isAuthenticated, isAdmin, hasAnyAdminPermission } = useAuthStore()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!isAdmin() && !hasAnyAdminPermission()) return <Navigate to="/" replace />
  return children
}

// Redirects custom-role staff away from the Admin-only dashboard to their first accessible page
function AdminEntry() {
  const { isAdmin, hasPermission } = useAuthStore()
  if (isAdmin()) return <AdminDashboard />
  const first = PERMISSION_TO_ROUTE.find(({ perm }) => hasPermission(perm))
  return <Navigate to={first?.route ?? '/'} replace />
}

function GuestOnly({ children }) {
  const { isAuthenticated } = useAuthStore()
  return !isAuthenticated ? children : <Navigate to="/" replace />
}

// ── App ────────────────────────────────────────────────────────────────────────
export default function App() {
  return (
    <HelmetProvider>
      <BrowserRouter>
        <ErrorBoundary>
          <Toaster
            position="top-right"
            toastOptions={{
              duration: 3500,
              style: {
                background: '#18181b',
                color: '#fafafa',
                borderRadius: '12px',
                fontSize: '14px',
                padding: '12px 16px',
              },
              success: { iconTheme: { primary: '#10b981', secondary: '#fff' } },
              error:   { iconTheme: { primary: '#ef4444', secondary: '#fff' } },
            }}
          />

          <Routes>
            {/* ── Storefront ── */}
            <Route path="/" element={<HomePage />} />
            <Route path="/products" element={<ProductsPage />} />
            <Route path="/products/:id" element={<ProductDetailPage />} />

            {/* ── Auth ── */}
            <Route path="/login"           element={<GuestOnly><LoginPage /></GuestOnly>} />
            <Route path="/register"        element={<GuestOnly><RegisterPage /></GuestOnly>} />
            <Route path="/forgot-password" element={<GuestOnly><ForgotPasswordPage /></GuestOnly>} />
            <Route path="/reset-password"  element={<ResetPasswordPage />} />

            {/* ── Customer account ── */}
            <Route path="/checkout"              element={<RequireAuth><CheckoutPage /></RequireAuth>} />
            <Route path="/account/orders"        element={<RequireAuth><MyOrdersPage /></RequireAuth>} />
            <Route path="/account/orders/:id"    element={<RequireAuth><OrderDetailPage /></RequireAuth>} />
            <Route path="/account/profile"       element={<RequireAuth><AccountProfilePage /></RequireAuth>} />
            <Route path="/account/wishlist"      element={<RequireAuth><WishlistPage /></RequireAuth>} />

            {/* ── Admin ── */}
            <Route path="/admin"            element={<RequireAdmin><AdminEntry /></RequireAdmin>} />
            <Route path="/admin/products"   element={<RequireAdmin><AdminProducts /></RequireAdmin>} />
            <Route path="/admin/orders"     element={<RequireAdmin><AdminOrders /></RequireAdmin>} />
            <Route path="/admin/inventory"  element={<RequireAdmin><AdminInventory /></RequireAdmin>} />
            <Route path="/admin/users"      element={<RequireAdmin><AdminUsers /></RequireAdmin>} />
            <Route path="/admin/reports"    element={<RequireAdmin><AdminReports /></RequireAdmin>} />
            <Route path="/admin/images"     element={<RequireAdmin><AdminImages /></RequireAdmin>} />
            <Route path="/admin/coupons"    element={<RequireAdmin><AdminCoupons /></RequireAdmin>} />
            <Route path="/admin/roles"      element={<RequireAdmin><AdminRoles /></RequireAdmin>} />

            {/* ── 404 ── */}
            <Route path="*" element={
              <div className="min-h-screen flex items-center justify-center flex-col gap-4">
                <h1 className="font-display text-6xl font-bold text-surface-200">404</h1>
                <p className="text-surface-500">Page not found</p>
                <a href="/" className="btn-primary">Go Home</a>
              </div>
            } />
          </Routes>
        </ErrorBoundary>
      </BrowserRouter>
    </HelmetProvider>
  )
}
