import { Link } from 'react-router-dom'
import { Navbar, Footer } from '../../components/layout/ShopLayout'
import { PageTitle } from '../../components/ui'
import { whatsAppLink } from '../../components/shop/WhatsApp'
import { INFO_PAGES } from './infoLinks'

// Terms, refunds, disclaimer, privacy and help (paths and names in infoLinks.js)

const UPDATED = '1 November 2026'
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

export function TermsPage() {
  return (
    <InfoPage page="terms"
      intro="These terms apply when you use this website or buy from Hytel Phones Ltd (“Hytel Phones”, “we”). By creating an account or placing an order, you agree to them.">
      <Section title="Who can buy">
        <p>You need to be 18 or over to create an account and place orders.</p>
      </Section>

      <Section title="Your account">
        <List>
          <li>Give your real name, a working email address and a phone number we can reach you on.</li>
          <li>Keep your password to yourself. You're responsible for orders placed from your account.</li>
          <li>We may suspend an account that's used for fraud, abuse or to break these terms.</li>
        </List>
      </Section>

      <Section title="Prices and products">
        <List>
          <li>Prices are in naira and can change. The price you pay is the one shown at checkout when you place the order.</li>
          <li>Photos and descriptions come from the manufacturers. Colours can look slightly different on screen.</li>
          <li>If a price or description is clearly wrong, we'll contact you before going ahead, and you can cancel for a full refund of anything you've paid.</li>
        </List>
      </Section>

      <Section title="Orders and payment">
        <List>
          <li>Pay by bank transfer to the account shown on your order. Use your order number or your name as the reference.</li>
          <li>Your order holds the phones for you for 24 hours. If we haven't received the payment by then, the order is cancelled automatically and the stock goes back on sale.</li>
          <li>We confirm your order once the payment has arrived, and you can follow each step under My Orders.</li>
          <li>We may cancel an order if the item is no longer available, the price was wrong or we suspect fraud. If you've paid, we refund the full amount.</li>
        </List>
      </Section>

      <Section title="Delivery and pickup">
        <List>
          <li>The delivery fee depends on your state and is shown at checkout before you place the order. If we don't deliver to your state yet, you can collect from one of our shops instead.</li>
          <li>Pickup is free. Choose the shop at checkout, and collect once we tell you the order is ready.</li>
          <li>Delivery times are estimates. Someone needs to be there to receive and check the order.</li>
        </List>
      </Section>

      <Section title="Checking your order, returns and warranty">
        <p>
          We don't accept returns or exchanges, so please check your phone before you accept it.
          The <To page="returns" /> explains how.
        </p>
        <p>
          Where a product page lists a warranty, it's given by the manufacturer and handled by their
          service centres. We'll help you find the right one.
        </p>
      </Section>

      <Section title="Coupons">
        <p>
          Each coupon has its own conditions, such as a minimum spend, an end date or a limit on how
          many times it can be used. Coupons can't be exchanged for cash. If an order with a coupon is
          cancelled, the coupon can be used again.
        </p>
      </Section>

      <Section title="Reviews">
        <p>
          Reviews must be honest and about the product. We may remove reviews that are abusive,
          misleading or off-topic. Reviews are customers' own opinions, not ours.
        </p>
      </Section>

      <Section title="Our responsibility to you">
        <p>
          As far as the law allows, we're not responsible for indirect losses, such as lost earnings
          or data. Nothing in these terms takes away your rights under Nigerian law, including the
          Federal Competition and Consumer Protection Act.
        </p>
      </Section>

      <Section title="Law and changes">
        <p>These terms are governed by the laws of the Federal Republic of Nigeria.</p>
        <p>We may update these terms. The version that applies to an order is the one shown when you placed it.</p>
      </Section>

      <Section title="Contact">
        <p>Questions about these terms: <Email /> or see <To page="help" />.</p>
      </Section>
    </InfoPage>
  )
}

export function ReturnsPage() {
  return (
    <InfoPage page="returns"
      intro="We don't accept returns or exchanges, including for a change of mind. Instead, you check your phone before you accept it, and anything wrong is sorted out there and then.">
      <Section title="Check before you accept">
        <p>When you collect from a shop, check the order with our staff before you leave. When it's delivered, open and check it in front of the delivery rider before you accept it. Make sure that:</p>
        <List>
          <li>it's the model, colour and storage size you ordered;</li>
          <li>the box and phone aren't damaged, and everything listed on the box is inside;</li>
          <li>the phone switches on and works.</li>
        </List>
      </Section>

      <Section title="If something is wrong">
        <p>
          Don't accept it. Tell the shop staff or the rider, and contact us straight away
          on <Email /> or WhatsApp. We'll replace it with the right item or refund you in full.
        </p>
      </Section>

      <Section title="Once you've accepted it">
        <p>
          Once you've accepted the order, it's yours. We can't take it back or swap it. If a fault
          appears later and the product page lists a warranty, the manufacturer's service centre
          handles it, and we'll help you find the right one.
        </p>
      </Section>

      <Section title="Refunds">
        <p>We refund in full, including any delivery fee, when:</p>
        <List>
          <li>we cancel an order you've paid for, for example because the item is no longer available;</li>
          <li>you refuse an order at delivery or pickup because it's wrong or faulty.</li>
        </List>
        <p>
          Refunds go back by bank transfer. We'll contact you to confirm the account to send it to.
          An order that's cancelled because it wasn't paid for within 24 hours has nothing to refund.
        </p>
      </Section>

      <Section title="Your rights">
        <p>This policy doesn't affect your rights under Nigerian law, including the Federal Competition and Consumer Protection Act.</p>
      </Section>
    </InfoPage>
  )
}

export function DisclaimerPage() {
  return (
    <InfoPage page="disclaimer">
      <Section title="Product information">
        <p>
          Specifications, photos and descriptions come from the manufacturers and can change without
          notice. We work to keep them accurate, but small differences are possible, for example in
          colour on screen. If something matters to you, ask us before you order.
        </p>
      </Section>

      <Section title="Prices and stock">
        <p>Prices and stock change often. What you see is correct when shown, and we'll correct any mistakes we find.</p>
      </Section>

      <Section title="Warranties">
        <p>Where a product page lists a warranty, the manufacturer gives it, not Hytel Phones.</p>
      </Section>

      <Section title="Reviews">
        <p>Reviews are the opinions of the customers who wrote them, not ours.</p>
      </Section>

      <Section title="This website">
        <p>
          We try to keep the site available and secure, but can't promise it will always be
          uninterrupted or free of errors. Links to other sites, such as WhatsApp, are there to help
          you. We're not responsible for those sites' content.
        </p>
      </Section>

      <Section title="Your rights">
        <p>Nothing here takes away your rights under Nigerian law. See also our <To page="terms" />.</p>
      </Section>
    </InfoPage>
  )
}

export function PrivacyPage() {
  return (
    <InfoPage page="privacy"
      intro="This policy explains what personal information Hytel Phones Ltd collects through this website, why, and your rights under the Nigeria Data Protection Act 2023.">
      <Section title="What we collect">
        <List>
          <li><strong>Your account:</strong> name, email address and password. The password is stored scrambled (hashed), so no one can read it, including us.</li>
          <li><strong>Your orders:</strong> phone number, delivery address or chosen pickup shop, what you bought, coupons used and payment references.</li>
          <li><strong>What you add:</strong> reviews, your wishlist and your cart.</li>
          <li><strong>Security:</strong> your IP address, used to limit repeated sign-in attempts.</li>
        </List>
        <p>We don't collect card details. Payments are made by bank transfer from your own bank.</p>
      </Section>

      <Section title="Why we use it">
        <List>
          <li>to process, deliver and keep you updated on your orders, including by email;</li>
          <li>to run your account, including password resets;</li>
          <li>to keep the site and your account secure and prevent fraud;</li>
          <li>to keep the sales records the law requires.</li>
        </List>
        <p>We don't sell your information, and we don't send marketing emails.</p>
      </Section>

      <Section title="Who we share it with">
        <List>
          <li><strong>Delivery partners:</strong> your name, phone number and address, only to deliver your order.</li>
          <li><strong>Microsoft Azure:</strong> hosts this website, its database and the emails it sends. The servers are outside Nigeria. Microsoft protects the data under its own security and privacy commitments.</li>
          <li><strong>Authorities:</strong> only when the law requires it.</li>
        </List>
      </Section>

      <Section title="Cookies and storage in your browser">
        <p>
          We use one cookie, which keeps you signed in. Your browser also stores your sign-in details
          and the products you viewed recently. There are no advertising or tracking cookies. The
          site's fonts load from Google, which sees your IP address when they do.
        </p>
      </Section>

      <Section title="How long we keep it">
        <p>
          We keep your account until you ask us to delete it. Order records are kept for as long as
          the law requires for accounting and tax, even after an account is deleted.
        </p>
      </Section>

      <Section title="Your rights">
        <p>Under the Nigeria Data Protection Act 2023 you can ask us to:</p>
        <List>
          <li>give you a copy of the information we hold about you;</li>
          <li>correct anything that's wrong;</li>
          <li>delete your information, unless we have to keep it by law;</li>
          <li>stop or limit how we use it.</li>
        </List>
        <p>
          Email <Email /> and we'll reply as quickly as we can, within the time the law allows. If you're not happy with our reply, you can
          complain to the Nigeria Data Protection Commission.
        </p>
      </Section>

      <Section title="Keeping it safe">
        <p>
          The site only works over an encrypted connection. Staff only see the information they need
          for their job. If a breach ever affected your information, we'd tell you and the Commission
          as the law requires.
        </p>
      </Section>

      <Section title="Changes">
        <p>We'll update this page if anything changes, and the date at the top will show when.</p>
      </Section>
    </InfoPage>
  )
}

const FAQ = [
  ['How do I pay?',
    <>Pay by bank transfer to the account shown on your order (My Orders, then open the order). Use your order number or your name as the reference. We confirm the order once the money arrives, usually within 1–2 business hours.</>],
  ['How long do I have to pay?',
    <>24 hours. Your order shows the exact time. After that it's cancelled automatically and the phones go back on sale, so if you still want them, place the order again.</>],
  ['How much is delivery?',
    <>It depends on your state, and checkout shows the fee before you order. If your state isn't listed, choose pickup from one of our shops. Pickup is free.</>],
  ['Where is my order?',
    <>Sign in and open <Link to="/account/orders" className="text-brand-600 hover:underline">My Orders</Link> to see each step: awaiting payment, payment received, being prepared, then on its way or ready for pickup. We also email you when it changes.</>],
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
