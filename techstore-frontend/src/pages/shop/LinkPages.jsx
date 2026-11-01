import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { CheckCircle, MailX } from 'lucide-react'
import { newsletterApi, orderApi } from '../../services/api'
import { Navbar, Footer } from '../../components/layout/ShopLayout'
import { PageTitle, Spinner } from '../../components/ui'

// Pages that emailed links open

function LinkPage({ title, children }) {
  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>{title}</PageTitle>
      <main className="flex-1 page-container py-16">
        <div className="max-w-md mx-auto text-center">{children}</div>
      </main>
      <Footer />
    </div>
  )
}

// The unsubscribe link in each newsletter (/newsletter/unsubscribe?token=…). It asks first:
// mail scanners open links on their own, and that shouldn't take anyone off the list.
export function UnsubscribePage() {
  const [params] = useSearchParams()
  const token = params.get('token')
  const [status, setStatus] = useState(token ? 'ask' : 'invalid') // ask | working | done | invalid

  const unsubscribe = async () => {
    setStatus('working')
    try { await newsletterApi.unsubscribe(token); setStatus('done') }
    catch { setStatus('invalid') }
  }

  return (
    <LinkPage title="Unsubscribe">
      {status === 'done' ? <>
        <CheckCircle className="w-12 h-12 text-emerald-500 mx-auto mb-4" />
        <h1 className="font-display text-2xl font-semibold">You've been unsubscribed</h1>
        <p className="text-surface-500 mt-2">You won't get our newsletter any more. You can sign up again at the bottom of any page.</p>
      </> : status === 'invalid' ? <>
        <MailX className="w-12 h-12 text-surface-300 mx-auto mb-4" />
        <h1 className="font-display text-2xl font-semibold">That link isn't valid</h1>
        <p className="text-surface-500 mt-2">Use the unsubscribe link from one of our newsletters, or contact us and we'll take you off the list.</p>
      </> : <>
        <h1 className="font-display text-2xl font-semibold">Unsubscribe from our newsletter?</h1>
        <p className="text-surface-500 mt-2">You'll stop getting news and offers from Hytel Phones.</p>
        <button onClick={unsubscribe} disabled={status === 'working'} className="btn-primary mt-6">
          {status === 'working' ? <Spinner size="sm" /> : 'Unsubscribe'}
        </button>
      </>}
    </LinkPage>
  )
}

// The link in the "add your earlier orders" email (/account/claim-orders?token=…). It only
// opens signed in, as the account the email was sent for.
export function ClaimOrdersPage() {
  const [params] = useSearchParams()
  const [result, setResult] = useState(null) // { claimed } or { error }

  useEffect(() => {
    orderApi.claim(params.get('token') ?? '')
      .then(r => setResult({ claimed: r.data.claimed }))
      .catch(err => setResult({ error: err.response?.data?.message ?? "We couldn't add your orders. Please try again." }))
  }, [])

  return (
    <LinkPage title="Add Your Orders">
      {!result ? <div className="flex justify-center"><Spinner /></div>
      : result.error ? <>
        <MailX className="w-12 h-12 text-surface-300 mx-auto mb-4" />
        <h1 className="font-display text-2xl font-semibold">That link didn't work</h1>
        <p className="text-surface-500 mt-2">{result.error}</p>
        <Link to="/account/orders" className="btn-primary mt-6">My Orders</Link>
      </> : <>
        <CheckCircle className="w-12 h-12 text-emerald-500 mx-auto mb-4" />
        <h1 className="font-display text-2xl font-semibold">
          {result.claimed === 1 ? '1 order added' : `${result.claimed} orders added`} to your account
        </h1>
        <p className="text-surface-500 mt-2">You'll find {result.claimed === 1 ? 'it' : 'them'} with your other orders.</p>
        <Link to="/account/orders" className="btn-primary mt-6">My Orders</Link>
      </>}
    </LinkPage>
  )
}
