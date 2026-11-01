import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { format } from 'date-fns'
import { policyApi } from '../../services/api'
import { Navbar, Footer } from '../../components/layout/ShopLayout'
import { PageTitle, Spinner } from '../../components/ui'
import { whatsAppLink } from '../../components/shop/WhatsApp'
import { PolicyText } from '../../components/shop/PolicyText'
import { INFO_PAGES } from './infoLinks'

// Terms, refunds, disclaimer, privacy and help (paths and names in infoLinks.js)

const UPDATED = '1 November 2026' // the help page's
const EMAIL = 'support@hytelphones.ng'

function InfoPage({ page, intro, children }) {
  const { label } = INFO_PAGES[page]
  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>{label}</PageTitle>
      <main className="flex-1 page-container py-10">
        <article className="max-w-3xl">
          <h1 className="font-display text-3xl font-semibold text-surface-900">{label}</h1>
          <p className="text-sm text-surface-400 mt-2">Last updated {UPDATED}</p>
          {intro && <p className="mt-6 text-surface-600 leading-relaxed">{intro}</p>}
          {children}
        </article>
      </main>
      <Footer />
    </div>
  )
}

const Section = ({ title, children }) => (
  <section className="mt-8">
    <h2 className="font-display text-lg font-semibold text-surface-900 mb-2">{title}</h2>
    <div className="space-y-3 text-surface-600 leading-relaxed">{children}</div>
  </section>
)
const List = ({ children }) => <ul className="list-disc pl-5 space-y-1.5">{children}</ul>
const To = ({ page, children }) => (
  <Link to={INFO_PAGES[page].path} className="text-brand-600 hover:underline">{children ?? INFO_PAGES[page].label}</Link>
)
const Email = () => <a href={`mailto:${EMAIL}`} className="text-brand-600 hover:underline">{EMAIL}</a>

// Terms, returns, privacy and disclaimer: the text is edited in Admin -> Policy Pages
function PolicyPage({ page }) {
  const { label } = INFO_PAGES[page]
  const [policy, setPolicy]   = useState(null) // { body, updatedAt }
  const [failed, setFailed]   = useState(false)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    setFailed(false)
    policyApi.get(page).then(r => setPolicy(r.data)).catch(() => setFailed(true))
  }, [page, attempt])

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <PageTitle>{label}</PageTitle>
      <main className="flex-1 page-container py-10">
        <article className="max-w-3xl">
          <h1 className="font-display text-3xl font-semibold text-surface-900">{label}</h1>
          {policy ? <>
            <p className="text-sm text-surface-400 mt-2">Last updated {format(new Date(policy.updatedAt), 'd MMMM yyyy')}</p>
            <div className="mt-4"><PolicyText>{policy.body}</PolicyText></div>
          </> : failed ? (
            <div className="mt-8">
              <p className="text-surface-600">This page couldn't be loaded.</p>
              <button onClick={() => setAttempt(a => a + 1)} className="btn-secondary mt-3">Try again</button>
            </div>
          ) : <div className="flex justify-center py-16"><Spinner /></div>}
        </article>
      </main>
      <Footer />
    </div>
  )
}

export const TermsPage      = () => <PolicyPage page="terms" />
export const ReturnsPage    = () => <PolicyPage page="returns" />
export const DisclaimerPage = () => <PolicyPage page="disclaimer" />
export const PrivacyPage    = () => <PolicyPage page="privacy" />

const FAQ = [
  ['Do I need an account?',
    <>No. You can check out as a guest with your email and phone number, and follow your order through the link we give you. An account keeps your details and all your orders in one place.</>],
  ['How do I pay?',
    <>Pay by bank transfer to the account shown on your order (My Orders, then open the order). Use your order number or your name as the reference. We confirm the order once the money arrives, usually within 1–2 business hours.</>],
  ['How long do I have to pay?',
    <>24 hours. Your order shows the exact time. After that it's cancelled automatically and the phones go back on sale, so if you still want them, place the order again.</>],
  ['How much is delivery?',
    <>It depends on your state, and checkout shows the fee before you order. If your state isn't listed, choose pickup from one of our shops. Pickup is free.</>],
  ['Where is my order?',
    <>Sign in and open <Link to="/account/orders" className="text-brand-600 hover:underline">My Orders</Link> to see each step: awaiting payment, payment received, being prepared, then on its way or ready for pickup. Ordered without an account? Open the link from your order, or use <Link to="/track-order" className="text-brand-600 hover:underline">Track an order</Link> with your order number and email. We also email you when it changes.</>],
  ['Can I cancel my order?',
    <>Contact us as soon as possible, before it's sent out or ready for pickup. If you haven't paid, you can also just not pay: the order is cancelled after 24 hours.</>],
  ['Can I return or exchange a phone?',
    <>No. We don't accept returns or exchanges, so please check your phone before you accept it. See the <To page="returns" />.</>],
  ['I forgot my password',
    <>Use <Link to="/forgot-password" className="text-brand-600 hover:underline">Forgot password</Link> on the sign-in page and we'll email you a link to set a new one.</>],
  ['My coupon doesn’t work',
    <>Coupons can have a minimum spend, an end date or a limit on how many times they can be used. The message you see when you apply it says which one applies.</>],
]

export function HelpPage() {
  return (
    <InfoPage page="help" intro="Questions about a phone or an order? We're happy to help.">
      <Section title="Contact us">
        <List>
          <li><strong>WhatsApp:</strong>{' '}
            <a href={whatsAppLink()} target="_blank" rel="noopener noreferrer" className="text-brand-600 hover:underline">0803 336 0284</a>
          </li>
          <li><strong>Phone:</strong> 0803 488 8246, Monday to Saturday, 8am to 6pm</li>
          <li><strong>Email:</strong> <Email /></li>
          <li><strong>In person:</strong> any of our shops. The addresses are at the bottom of this page.</li>
        </List>
        <p>Have your order number ready if your question is about an order. It starts with TS- and it's on your order and in our emails.</p>
      </Section>

      <Section title="Common questions">
        <div className="divide-y divide-surface-200 border-y border-surface-200">
          {FAQ.map(([q, a]) => (
            <details key={q} className="group py-3">
              <summary className="cursor-pointer list-none flex items-center justify-between gap-4 font-medium text-surface-900">
                {q}
                <span className="text-surface-400 transition-transform group-open:rotate-45 text-xl leading-none">+</span>
              </summary>
              <p className="mt-2 text-surface-600 leading-relaxed">{a}</p>
            </details>
          ))}
        </div>
      </Section>

      <Section title="Policies">
        <List>
          {['terms', 'returns', 'privacy', 'disclaimer'].map(p => <li key={p}><To page={p} /></li>)}
        </List>
      </Section>
    </InfoPage>
  )
}
