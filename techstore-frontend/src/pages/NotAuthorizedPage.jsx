import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Helmet } from 'react-helmet-async'
import { ShieldAlert } from 'lucide-react'
import { authApi } from '../services/api'
import { useAuthStore, useCartStore } from '../store'
import { returnHere } from '../utils/signIn'

// Shown in place of a back-office page the person signed in can't use, at the same address, so
// signing in as someone who can brings them straight back to it.
//   staff:      their role doesn't include this page (shown inside the back office, with their menu)
//   otherwise:  a customer account, and the back office is for staff
export function NotAuthorizedMessage({ staff = false, backOffice = null }) {
  const { user, logout } = useAuthStore()
  const navigate = useNavigate()
  const location = useLocation()

  const signInAsSomeoneElse = async () => {
    try { await authApi.logout() } catch { /* the cookie is cleared on the server regardless */ }
    logout()
    useCartStore.getState().clearLocal() // as the shop's sign-out: the next person doesn't get this cart
    navigate('/login', { state: returnHere(location) })
  }

  return (
    <div className="card max-w-md mx-auto p-6 sm:p-8 text-center space-y-4">
      <Helmet>
        <title>Not authorized · Hytel Phones</title>
        <meta name="robots" content="noindex" />
      </Helmet>
      <div className="w-14 h-14 rounded-2xl bg-amber-50 flex items-center justify-center mx-auto">
        <ShieldAlert className="w-7 h-7 text-amber-500" />
      </div>
      <div className="space-y-2">
        <h1 className="font-display text-xl font-semibold text-surface-900">You don’t have access to this page</h1>
        <p className="text-sm text-surface-500">
          {staff
            ? <>Your role{user?.role ? <> (<strong className="text-surface-700">{user.role}</strong>)</> : null} doesn’t include this page. If you need it, ask an Admin to add it to your role under Roles.</>
            : 'This part of the site is for Hytel Phones staff.'}
        </p>
        {user?.email && <p className="text-xs text-surface-400">Signed in as {user.email}</p>}
      </div>
      <div className="flex flex-col sm:flex-row sm:justify-center gap-2 pt-1">
        {staff && backOffice
          ? <Link to={backOffice} className="btn-primary">Go to my pages</Link>
          : <Link to="/" className="btn-primary">Back to the shop</Link>}
        <button type="button" className="btn-secondary" onClick={signInAsSomeoneElse}>Sign in as someone else</button>
      </div>
    </div>
  )
}

// For customers (and anyone without a back-office page): a page of its own
export default function NotAuthorizedPage() {
  return (
    <div className="min-h-screen bg-surface-50 flex items-center justify-center px-4 py-12">
      <div className="w-full">
        <Link to="/" className="flex justify-center mb-6">
          <img src="/hytel-logo.png" alt="Hytel Phones" className="h-12 w-auto object-contain" />
        </Link>
        <NotAuthorizedMessage />
      </div>
    </div>
  )
}
