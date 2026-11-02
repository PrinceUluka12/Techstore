import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, Navigate, useLocation } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'
import { HelmetProvider } from 'react-helmet-async'
import { useAuthStore } from './store'
import ErrorBoundary from './components/ErrorBoundary'
import { LoadingPage } from './components/ui'
import { returnHere } from './utils/signIn'

// Shop pages
import HomePage from './pages/shop/HomePage'
import ProductsPage from './pages/shop/ProductsPage'
import ProductDetailPage from './pages/shop/ProductDetailPage'
import { LoginPage, RegisterPage, CheckoutPage } from './pages/shop/AuthPages'
import { MyOrdersPage, OrderDetailPage, GuestOrderPage, TrackOrderPage } from './pages/shop/OrdersPage'
import ForgotPasswordPage from './pages/shop/ForgotPasswordPage'
import ResetPasswordPage from './pages/shop/ResetPasswordPage'
import AccountProfilePage from './pages/shop/AccountProfilePage'
import WishlistPage from './pages/shop/WishlistPage'
import { INFO_PAGES } from './pages/shop/infoLinks'
import { UnsubscribePage, ClaimOrdersPage } from './pages/shop/LinkPages'
import NotAuthorizedPage from './pages/NotAuthorizedPage'

// Admin pages are loaded only when someone opens one, so shoppers don't download the back
// office (the till, reports, charts) with the shop
const AdminDashboard    = lazy(() => import('./pages/admin/AdminDashboard'))
const AdminProducts     = lazy(() => import('./pages/admin/AdminProducts'))
const AdminOrders       = lazy(() => import('./pages/admin/AdminOrders'))
const AdminInventory    = lazy(() => import('./pages/admin/AdminInventory'))
const AdminUsers        = lazy(() => import('./pages/admin/AdminUsersReports').then(m => ({ default: m.AdminUsers })))
const AdminReports      = lazy(() => import('./pages/admin/AdminUsersReports').then(m => ({ default: m.AdminReports })))
const AdminImages       = lazy(() => import('./pages/admin/AdminImages'))
const AdminCoupons      = lazy(() => import('./pages/admin/AdminCoupons'))
const AdminRoles        = lazy(() => import('./pages/admin/AdminRoles'))
const AdminStores       = lazy(() => import('./pages/admin/AdminStores'))
const AdminPos          = lazy(() => import('./pages/admin/AdminPos'))
const AdminPosReceipt   = lazy(() => import('./pages/admin/AdminPosReceipt'))
const AdminCashUp       = lazy(() => import('./pages/admin/AdminCashUp'))
const AdminTransfers    = lazy(() => import('./pages/admin/AdminTransfers'))
const AdminBankAccounts = lazy(() => import('./pages/admin/AdminBankAccounts'))
const AdminDeliveryFees = lazy(() => import('./pages/admin/AdminDeliveryFees'))
const AdminNewsletter   = lazy(() => import('./pages/admin/AdminNewsletter'))
const AdminPolicies     = lazy(() => import('./pages/admin/AdminPolicies'))
const AdminApprovals    = lazy(() => import('./pages/admin/AdminApprovals'))
const AdminNotAuthorized = lazy(() => import('./pages/admin/AdminNotAuthorized'))

// Policies and help are rarely opened, so they load on demand too
const infoPage = name => lazy(() => import('./pages/shop/InfoPages').then(m => ({ default: m[name] })))
const InfoRoutes = {
  terms: infoPage('TermsPage'), returns: infoPage('ReturnsPage'), disclaimer: infoPage('DisclaimerPage'),
  privacy: infoPage('PrivacyPage'), help: infoPage('HelpPage'),
}

// ── Route guards ──────────────────────────────────────────────────────────────
// Signed-out visitors go to sign in, then come back here (utils/signIn)
function RequireAuth({ children }) {
  const { isAuthenticated } = useAuthStore()
  const location = useLocation()
  return isAuthenticated ? children : <Navigate to="/login" replace state={returnHere(location)} />
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
  { perm: 'stores.manage',    route: '/admin/shops' },
  { perm: 'pos.sell',         route: '/admin/pos' },
  { perm: 'pos.discount.approve', route: '/admin/approvals' },
  { perm: 'transfers.manage', route: '/admin/transfers' },
  { perm: 'pages.manage',     route: '/admin/policies' },
]

// The first back-office page this person can use, or null
const firstStaffRoute = hasPermission => PERMISSION_TO_ROUTE.find(({ perm }) => hasPermission(perm))?.route ?? null

// A back-office page: signed-out visitors sign in first. Someone signed in who can't use it sees
// "You don't have access" at the same address (customers on a page of its own, staff inside the
// back office with their menu), rather than being sent elsewhere or shown a page whose data the
// API then refuses. perms: any one of them will do (as in the menus); none listed is any staff
// (the /admin entry, which sends them on). Admins can use every page.
function RequireAdmin({ children, perms, adminOnly = false }) {
  const { isAuthenticated, isAdmin, hasPermission, hasAnyAdminPermission } = useAuthStore()
  const location = useLocation()
  if (!isAuthenticated) return <Navigate to="/login" replace state={returnHere(location)} />
  if (!isAdmin() && !hasAnyAdminPermission()) return <NotAuthorizedPage />
  const allowed = isAdmin() || (!adminOnly && (!perms || perms.some(p => hasPermission(p))))
  return allowed ? children : <AdminNotAuthorized backOffice={firstStaffRoute(hasPermission)} />
}

// Redirects custom-role staff away from the Admin-only dashboard to their first accessible page
function AdminEntry() {
  const { isAdmin, hasPermission } = useAuthStore()
  if (isAdmin()) return <AdminDashboard />
  const first = firstStaffRoute(hasPermission)
  return first ? <Navigate to={first} replace /> : <AdminNotAuthorized />
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

          <Suspense fallback={<LoadingPage />}>
          <Routes>
            {/* ── Storefront ── */}
            <Route path="/" element={<HomePage />} />
            <Route path="/products" element={<ProductsPage />} />
            <Route path="/products/:id" element={<ProductDetailPage />} />
            {/* No account needed: guests check out with an email and follow the order by its private link */}
            <Route path="/checkout" element={<CheckoutPage />} />
            <Route path="/orders/track/:token" element={<GuestOrderPage />} />
            <Route path="/track-order" element={<TrackOrderPage />} />
            <Route path="/newsletter/unsubscribe" element={<UnsubscribePage />} />
            {Object.entries(InfoRoutes).map(([key, Page]) =>
              <Route key={key} path={INFO_PAGES[key].path} element={<Page />} />)}

            {/* ── Auth ── */}
            <Route path="/login"           element={<GuestOnly><LoginPage /></GuestOnly>} />
            <Route path="/register"        element={<GuestOnly><RegisterPage /></GuestOnly>} />
            <Route path="/forgot-password" element={<GuestOnly><ForgotPasswordPage /></GuestOnly>} />
            <Route path="/reset-password"  element={<ResetPasswordPage />} />

            {/* ── Customer account ── */}
            <Route path="/account/orders"        element={<RequireAuth><MyOrdersPage /></RequireAuth>} />
            <Route path="/account/orders/:id"    element={<RequireAuth><OrderDetailPage /></RequireAuth>} />
            <Route path="/account/profile"       element={<RequireAuth><AccountProfilePage /></RequireAuth>} />
            <Route path="/account/wishlist"      element={<RequireAuth><WishlistPage /></RequireAuth>} />
            <Route path="/account/claim-orders"  element={<RequireAuth><ClaimOrdersPage /></RequireAuth>} />

            {/* ── Admin ── */}
            <Route path="/admin"            element={<RequireAdmin><AdminEntry /></RequireAdmin>} />
            <Route path="/admin/products"   element={<RequireAdmin perms={['products.manage']}><AdminProducts /></RequireAdmin>} />
            <Route path="/admin/orders"     element={<RequireAdmin perms={['orders.view', 'orders.manage']}><AdminOrders /></RequireAdmin>} />
            <Route path="/admin/inventory"  element={<RequireAdmin perms={['inventory.manage']}><AdminInventory /></RequireAdmin>} />
            {/* Same page component, so the keys keep each list's page number apart */}
            <Route path="/admin/users"      element={<RequireAdmin key="customers" perms={['users.view', 'users.manage']}><AdminUsers /></RequireAdmin>} />
            <Route path="/admin/staff"      element={<RequireAdmin key="staff" perms={['users.view', 'users.manage']}><AdminUsers staff /></RequireAdmin>} />
            <Route path="/admin/reports"    element={<RequireAdmin perms={['reports.view']}><AdminReports /></RequireAdmin>} />
            <Route path="/admin/images"     element={<RequireAdmin perms={['images.manage']}><AdminImages /></RequireAdmin>} />
            <Route path="/admin/coupons"    element={<RequireAdmin perms={['coupons.manage']}><AdminCoupons /></RequireAdmin>} />
            <Route path="/admin/roles"      element={<RequireAdmin perms={['roles.manage']}><AdminRoles /></RequireAdmin>} />
            <Route path="/admin/shops"      element={<RequireAdmin perms={['stores.manage']}><AdminStores /></RequireAdmin>} />
            <Route path="/admin/pos"        element={<RequireAdmin perms={['pos.sell']}><AdminPos /></RequireAdmin>} />
            <Route path="/admin/pos/sales/:id" element={<RequireAdmin perms={['pos.sell']}><AdminPosReceipt /></RequireAdmin>} />
            <Route path="/admin/pos/cash-up" element={<RequireAdmin perms={['pos.sell']}><AdminCashUp /></RequireAdmin>} />
            <Route path="/admin/approvals"  element={<RequireAdmin perms={['pos.discount.approve']}><AdminApprovals /></RequireAdmin>} />
            <Route path="/admin/transfers"  element={<RequireAdmin perms={['transfers.manage']}><AdminTransfers /></RequireAdmin>} />
            <Route path="/admin/bank-accounts" element={<RequireAdmin adminOnly><AdminBankAccounts /></RequireAdmin>} />
            <Route path="/admin/delivery-fees" element={<RequireAdmin adminOnly><AdminDeliveryFees /></RequireAdmin>} />
            <Route path="/admin/newsletter" element={<RequireAdmin perms={['users.view']}><AdminNewsletter /></RequireAdmin>} />
            <Route path="/admin/policies"   element={<RequireAdmin perms={['pages.manage']}><AdminPolicies /></RequireAdmin>} />

            {/* ── 404 ── */}
            <Route path="*" element={
              <div className="min-h-screen flex items-center justify-center flex-col gap-4">
                <h1 className="font-display text-6xl font-bold text-surface-200">404</h1>
                <p className="text-surface-500">Page not found</p>
                <a href="/" className="btn-primary">Go Home</a>
              </div>
            } />
          </Routes>
          </Suspense>
        </ErrorBoundary>
      </BrowserRouter>
    </HelmetProvider>
  )
}
