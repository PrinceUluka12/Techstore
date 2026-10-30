import { useEffect, useRef, useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { ScanBarcode, Plus, Minus, Trash2, Smartphone, ShieldCheck, Package } from 'lucide-react'
import toast from 'react-hot-toast'
import { authApi, posApi, storeApi } from '../../services/api'
import { useAuthStore } from '../../store'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, Spinner } from '../../components/ui'
import { isValidImei, EMAIL_RE, NG_PHONE_RE, PIN_RE, normalizePhone, showApiError } from '../../utils/validation'

const VAT_RATE = 0.075 // same as PosService / online checkout
const METHODS = [
  { id: 'Cash',         label: 'Cash' },
  { id: 'Card',         label: 'Card (POS terminal)' },
  { id: 'BankTransfer', label: 'Bank transfer' },
  { id: 'USSD',         label: 'USSD' },
  { id: 'Other',        label: 'Other' },
]
const NEEDS_REFERENCE = ['Card', 'BankTransfer', 'USSD']

const round2 = n => Math.round(n * 100) / 100
export const naira = n => `₦${Number(n || 0).toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
let nextKey = 1
const newPayment = (method = 'Cash') => ({ key: nextKey++, method, amount: '', reference: '' })

// ── Product search / barcode scan ─────────────────────────────────────────────
function ProductSearch({ storeId, onAdd }) {
  const [query, setQuery]     = useState('')
  const [results, setResults] = useState([])
  const [loading, setLoading] = useState(false)
  const inputRef = useRef()

  const search = (q) => {
    setLoading(true)
    return posApi.products(q, storeId)
      .then(r => { setResults(r.data); return r.data })
      .catch(err => { showApiError(err, null, 'Search failed'); return [] })
      .finally(() => setLoading(false))
  }

  useEffect(() => {
    if (!storeId) return
    const t = setTimeout(() => search(query.trim()), 250)
    return () => clearTimeout(t)
  }, [query, storeId])

  // A barcode scanner types the SKU and presses Enter: add an exact match straight away
  const onKeyDown = async (e) => {
    if (e.key !== 'Enter') return
    e.preventDefault()
    const q = query.trim()
    if (!q) return
    const found = await search(q)
    const exact = found.find(p => p.sku.toLowerCase() === q.toLowerCase())
    if (exact) { onAdd(exact); setQuery('') }
    else if (found.length === 1) { onAdd(found[0]); setQuery('') }
  }

  return (
    <div className="card p-4 space-y-3">
      <div className="relative">
        <ScanBarcode className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400" />
        <input ref={inputRef} autoFocus className="input pl-9" value={query} aria-label="Scan or search products"
          placeholder="Scan a barcode or search by name / SKU…"
          onChange={e => setQuery(e.target.value)} onKeyDown={onKeyDown} />
      </div>
      <div className="max-h-64 overflow-y-auto divide-y divide-surface-50 -mx-1">
        {loading && results.length === 0 ? (
          <div className="flex justify-center py-6"><Spinner /></div>
        ) : results.length === 0 ? (
          <p className="text-center text-sm text-surface-400 py-6">No products found</p>
        ) : results.map(p => (
          <button key={p.id} type="button" onClick={() => { onAdd(p); inputRef.current?.focus() }}
            disabled={p.quantityAvailable <= 0}
            className="w-full flex items-center gap-3 px-1 py-2 text-left hover:bg-surface-50 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed">
            <div className="w-9 h-9 rounded-lg bg-surface-50 flex items-center justify-center flex-shrink-0 overflow-hidden">
              {p.imageUrl ? <img src={p.imageUrl} alt="" className="w-full h-full object-contain" /> : <Package className="w-4 h-4 text-surface-300" />}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-surface-900 truncate">{p.name}</p>
              <p className="text-xs text-surface-400 font-mono">{p.sku}</p>
            </div>
            <div className="text-right flex-shrink-0">
              <p className="text-sm font-semibold">{naira(p.price)}</p>
              <p className={`text-xs ${p.quantityAvailable > 0 ? 'text-surface-400' : 'text-red-500'}`}>
                {p.quantityAvailable > 0 ? `${p.quantityAvailable} in stock` : 'Out of stock'}
              </p>
            </div>
          </button>
        ))}
      </div>
    </div>
  )
}

// ── One line of the sale ──────────────────────────────────────────────────────
function SaleLine({ line, errors, onChange, onRemove }) {
  const { product } = line
  const [editingPrice, setEditingPrice] = useState(line.unitPrice !== product.price)

  const setQty = (qty) => {
    const quantity = Math.max(1, Math.min(qty, product.quantityAvailable, 99))
    const serials = Array.from({ length: quantity }, (_, i) => line.serials[i] ?? '')
    onChange({ ...line, quantity, serials })
  }
  const priceChanged = line.unitPrice !== product.price

  return (
    <div className="py-3 space-y-2">
      <div className="flex items-start gap-3">
        <div className="flex-1 min-w-0">
          <p className="font-medium text-surface-900 truncate">{product.name}</p>
          <p className="text-xs text-surface-400">
            <span className="font-mono">{product.sku}</span> · {product.quantityAvailable} in stock
          </p>
        </div>
        <div className="flex items-center gap-1">
          <button type="button" className="btn-icon" onClick={() => setQty(line.quantity - 1)} aria-label="One less"><Minus className="w-3.5 h-3.5" /></button>
          <span className="w-8 text-center font-semibold">{line.quantity}</span>
          <button type="button" className="btn-icon" onClick={() => setQty(line.quantity + 1)} aria-label="One more"
            disabled={line.quantity >= product.quantityAvailable}><Plus className="w-3.5 h-3.5" /></button>
        </div>
        <div className="w-28 text-right">
          <p className="font-semibold">{naira(line.unitPrice * line.quantity)}</p>
          {priceChanged && <p className="text-xs text-surface-400 line-through">{naira(product.price * line.quantity)}</p>}
        </div>
        <button type="button" className="btn-icon text-red-400 hover:bg-red-50" onClick={onRemove} aria-label="Remove line">
          <Trash2 className="w-3.5 h-3.5" />
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-2 text-xs">
        {editingPrice ? (
          <label className="flex items-center gap-2">
            <span className="text-surface-500">Unit price ₦</span>
            <input type="number" min="0" step="0.01" className="input py-1 w-32 text-sm"
              value={line.unitPrice} aria-label="Unit price"
              onChange={e => onChange({ ...line, unitPrice: e.target.value === '' ? 0 : Number(e.target.value) })} />
            <button type="button" className="text-brand-600 hover:underline"
              onClick={() => { onChange({ ...line, unitPrice: product.price }); setEditingPrice(false) }}>Reset</button>
            {priceChanged && <span className="text-amber-600">Needs manager approval</span>}
          </label>
        ) : (
          <button type="button" className="text-brand-600 hover:underline" onClick={() => setEditingPrice(true)}>
            Change price
          </button>
        )}
      </div>
      {errors.price && <p className="text-red-500 text-xs">{errors.price}</p>}

      {product.requiresImei && (
        <div className="grid sm:grid-cols-2 gap-2">
          {line.serials.map((s, i) => (
            <div key={i}>
              <div className="relative">
                <Smartphone className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-surface-400" />
                <input className={`input pl-8 py-1.5 text-sm font-mono ${errors.serials?.[i] ? 'border-red-400 focus:ring-red-400' : ''}`}
                  inputMode="numeric" maxLength={15} placeholder={`IMEI ${line.quantity > 1 ? i + 1 : ''}`.trim()}
                  aria-label={`IMEI ${i + 1}`} value={s}
                  onChange={e => {
                    const serials = [...line.serials]; serials[i] = e.target.value.replace(/\D/g, '')
                    onChange({ ...line, serials })
                  }} />
              </div>
              {errors.serials?.[i] && <p className="text-red-500 text-xs mt-0.5">{errors.serials[i]}</p>}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ── Manager approval ──────────────────────────────────────────────────────────
function ApprovalModal({ open, onClose, onApprove, submitting }) {
  const [approvers, setApprovers] = useState([])
  const [approverId, setApproverId] = useState('')
  const [pin, setPin] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (!open) return
    setPin(''); setError('')
    posApi.approvers().then(r => {
      setApprovers(r.data)
      setApproverId(id => id || String(r.data.find(a => a.hasPin)?.id ?? ''))
    }).catch(() => toast.error('Failed to load managers'))
  }, [open])

  const submit = (e) => {
    e.preventDefault()
    if (!approverId) return setError('Choose who is approving')
    if (!PIN_RE.test(pin)) return setError('The PIN is 4–6 digits')
    setError('')
    onApprove({ approverUserId: Number(approverId), pin }, setError)
  }

  return (
    <Modal open={open} onClose={onClose} title="Manager approval" size="sm">
      <form onSubmit={submit} noValidate className="space-y-4">
        <p className="text-sm text-surface-600 flex items-start gap-2">
          <ShieldCheck className="w-4 h-4 text-brand-500 flex-shrink-0 mt-0.5" />
          This sale has a discount or price change. A manager enters their PIN to approve it.
        </p>
        <div>
          <label className="label">Approved by</label>
          <select className="input" value={approverId} onChange={e => setApproverId(e.target.value)} aria-label="Approved by">
            <option value="">Choose a manager…</option>
            {approvers.map(a => (
              <option key={a.id} value={a.id} disabled={!a.hasPin}>{a.name}{a.hasPin ? '' : ' (no PIN set)'}</option>
            ))}
          </select>
          {approvers.length > 0 && !approvers.some(a => a.hasPin) && (
            <p className="text-xs text-surface-400 mt-1">No manager has a PIN yet. They can set one under My Account → Profile.</p>
          )}
        </div>
        <div>
          <label className="label">PIN</label>
          <input className="input tracking-[0.4em] font-mono" type="password" inputMode="numeric" maxLength={6}
            autoComplete="off" aria-label="Manager PIN" value={pin}
            onChange={e => setPin(e.target.value.replace(/\D/g, ''))} />
        </div>
        {error && <p className="text-red-500 text-sm">{error}</p>}
        <div className="flex justify-end gap-3">
          <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
          <button type="submit" className="btn-primary" disabled={submitting}>
            {submitting ? <Spinner size="sm" /> : 'Approve & complete sale'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

// ── Till page ─────────────────────────────────────────────────────────────────
export default function AdminPos() {
  const navigate = useNavigate()
  const { isAdmin } = useAuthStore()
  const admin = isAdmin()

  const [stores, setStores]   = useState([])
  const [storeId, setStoreId] = useState(null)
  const [homeStoreId, setHomeStoreId] = useState(undefined) // undefined = still loading

  const [lines, setLines]       = useState([])
  const [discount, setDiscount] = useState('')
  const [reason, setReason]     = useState('')
  const [customer, setCustomer] = useState({ name: '', phone: '', email: '' })
  const [payments, setPayments] = useState([newPayment()])
  const [errors, setErrors]     = useState({})
  const [approvalOpen, setApprovalOpen] = useState(false)
  const [submitting, setSubmitting]     = useState(false)

  useEffect(() => {
    Promise.all([authApi.getMe(), storeApi.getAll()]).then(([me, s]) => {
      const active = s.data.filter(x => x.isActive)
      setStores(active)
      setHomeStoreId(me.data.storeId ?? null)
      // Admins may sell for any shop; start on their home shop or the online shop
      setStoreId(me.data.storeId ?? (admin ? active.find(x => x.sendsOnlineOrders)?.id ?? active[0]?.id : null))
    }).catch(() => toast.error('Failed to load the till'))
  }, [])

  const shop = stores.find(s => s.id === storeId)

  // ── Totals (the API works them out again and is the one that counts) ──
  const subTotal     = round2(lines.reduce((sum, l) => sum + l.unitPrice * l.quantity, 0))
  const discountNum  = round2(Number(discount) || 0)
  const tax          = round2((subTotal - discountNum) * VAT_RATE)
  const total        = round2(subTotal - discountNum + tax)
  const paid         = round2(payments.reduce((sum, p) => sum + (Number(p.amount) || 0), 0))
  const cashPaid     = round2(payments.filter(p => p.method === 'Cash').reduce((sum, p) => sum + (Number(p.amount) || 0), 0))
  const remaining    = round2(total - paid)
  const change       = remaining < 0 ? -remaining : 0
  const priceChanged = lines.some(l => l.unitPrice !== l.product.price)
  const needsApproval = discountNum > 0 || priceChanged

  const addProduct = (p) => {
    if (p.quantityAvailable <= 0) return toast.error(`${p.name} is out of stock at this shop`)
    setLines(prev => {
      const existing = prev.find(l => l.product.id === p.id)
      if (existing) {
        if (existing.quantity >= p.quantityAvailable) { toast.error(`Only ${p.quantityAvailable} in stock`); return prev }
        return prev.map(l => l === existing ? { ...l, quantity: l.quantity + 1, serials: [...l.serials, ''] } : l)
      }
      return [...prev, { key: nextKey++, product: p, quantity: 1, unitPrice: p.price, serials: [''] }]
    })
  }

  const updatePayment = (key, changes) => setPayments(prev => prev.map(p => p.key === key ? { ...p, ...changes } : p))

  const resetSale = () => {
    setLines([]); setDiscount(''); setReason(''); setCustomer({ name: '', phone: '', email: '' })
    setPayments([newPayment()]); setErrors({})
  }

  // Same rules as PosService, so most problems show before anything is sent
  const validate = () => {
    const e = { lines: {} }
    if (lines.length === 0) e.sale = 'Add at least one product'
    const seen = new Set()
    lines.forEach(l => {
      const le = {}
      if (!(l.unitPrice > 0)) le.price = 'Price must be more than 0'
      if (l.product.requiresImei) {
        le.serials = l.serials.map(s => {
          if (!s) return 'IMEI required'
          if (!isValidImei(s)) return 'Not a valid IMEI'
          if (seen.has(s)) return 'Entered twice'
          seen.add(s); return null
        })
        if (!le.serials.some(Boolean)) delete le.serials
      }
      if (Object.keys(le).length) e.lines[l.key] = le
    })
    if (discountNum < 0) e.discount = 'Discount can’t be negative'
    else if (discountNum > subTotal) e.discount = 'Discount can’t be more than the subtotal'
    if (needsApproval && !reason.trim()) e.reason = 'Give a reason for the discount or price change'
    if (customer.phone.trim() && !NG_PHONE_RE.test(normalizePhone(customer.phone))) e.phone = 'Enter a Nigerian number, e.g. 08012345678'
    if (customer.email.trim() && !EMAIL_RE.test(customer.email.trim())) e.email = 'Enter a valid email address'
    payments.forEach(p => {
      if (!(Number(p.amount) > 0)) e[`pay${p.key}`] = 'Enter an amount'
      else if (NEEDS_REFERENCE.includes(p.method) && !p.reference.trim()) e[`pay${p.key}`] = 'Enter the reference'
    })
    if (remaining > 0) e.payments = `${naira(remaining)} still to pay`
    else if (change > cashPaid) e.payments = 'Only cash can be more than what’s due'
    if (!Object.keys(e.lines).length) delete e.lines
    setErrors(e)
    return Object.keys(e).length === 0
  }

  const submitSale = async (approval, setApprovalError) => {
    setSubmitting(true)
    try {
      // The API only lets non-admins sell from their home shop, which is what storeId is for them
      const { data } = await posApi.createSale({
        storeId,
        items: lines.map(l => ({
          productId: l.product.id,
          quantity: l.quantity,
          unitPrice: l.unitPrice !== l.product.price ? l.unitPrice : undefined,
          serialNumbers: l.product.requiresImei ? l.serials : undefined,
        })),
        payments: payments.map(p => ({ method: p.method, amount: Number(p.amount), reference: p.reference.trim() || null })),
        discountAmount: discountNum,
        discountReason: needsApproval ? reason.trim() : null,
        approval,
        customerName:  customer.name.trim() || null,
        customerPhone: normalizePhone(customer.phone) || null,
        customerEmail: customer.email.trim() || null,
      })
      toast.success(`Sale ${data.orderNumber} completed`)
      resetSale()
      navigate(`/admin/pos/sales/${data.id}`)
    } catch (err) {
      const message = err.response?.data?.message ?? ''
      // A PIN problem stays in the approval dialog so the manager can try again
      if (approval && /PIN|approve/i.test(message)) setApprovalError(message)
      else { setApprovalOpen(false); showApiError(err, null, 'Sale failed') }
    } finally { setSubmitting(false) }
  }

  const complete = () => {
    if (!validate()) return toast.error('Check the highlighted fields')
    if (needsApproval) setApprovalOpen(true)
    else submitSale(null)
  }

  if (homeStoreId === undefined) return <AdminLayout><div className="flex justify-center py-20"><Spinner size="lg" /></div></AdminLayout>

  if (!storeId) return (
    <AdminLayout>
      <div className="card p-8 max-w-lg text-center space-y-2">
        <h1 className="font-display text-xl font-semibold">No home shop</h1>
        <p className="text-surface-500 text-sm">You need a home shop to use the till. An admin can set it on the Customers page.</p>
      </div>
    </AdminLayout>
  )

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Point of Sale</h1>
            <p className="text-surface-400 text-sm mt-0.5">Selling from <strong className="text-surface-700">{shop?.name}</strong></p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <Link to="/admin/pos/cash-up" className="btn-secondary text-sm">End-of-day cash-up</Link>
            {admin && (
              <select className="input w-auto text-sm" value={storeId} aria-label="Shop" disabled={lines.length > 0}
                title={lines.length > 0 ? 'Finish or clear the sale to change shop' : undefined}
                onChange={e => setStoreId(Number(e.target.value))}>
                {stores.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            )}
          </div>
        </div>

        <div className="grid lg:grid-cols-5 gap-6">
          {/* ── Left: products and the sale ── */}
          <div className="lg:col-span-3 space-y-4">
            <ProductSearch storeId={storeId} onAdd={addProduct} />

            <div className="card p-4">
              <div className="flex items-center justify-between mb-1">
                <h2 className="font-semibold text-surface-900">Sale</h2>
                {lines.length > 0 && <button type="button" className="text-xs text-red-500 hover:underline" onClick={resetSale}>Clear sale</button>}
              </div>
              {lines.length === 0 ? (
                <p className="text-sm text-surface-400 py-8 text-center">{errors.sale ?? 'Scan or pick a product to start'}</p>
              ) : (
                <div className="divide-y divide-surface-100">
                  {lines.map(l => (
                    <SaleLine key={l.key} line={l} errors={errors.lines?.[l.key] ?? {}}
                      onChange={next => setLines(prev => prev.map(x => x.key === l.key ? next : x))}
                      onRemove={() => setLines(prev => prev.filter(x => x.key !== l.key))} />
                  ))}
                </div>
              )}
            </div>

            <div className="card p-4 grid sm:grid-cols-3 gap-3">
              <div>
                <label className="label">Discount (₦)</label>
                <input type="number" min="0" step="0.01" className={`input ${errors.discount ? 'border-red-400' : ''}`}
                  placeholder="0" value={discount} onChange={e => setDiscount(e.target.value)} />
                {errors.discount && <p className="text-red-500 text-xs mt-1">{errors.discount}</p>}
              </div>
              <div className="sm:col-span-2">
                <label className="label">Reason {needsApproval ? '' : <span className="text-surface-400 font-normal">(for a discount or price change)</span>}</label>
                <input className={`input ${errors.reason ? 'border-red-400' : ''}`} maxLength={200}
                  placeholder="e.g. Loyal customer, price match" value={reason} onChange={e => setReason(e.target.value)} />
                {errors.reason && <p className="text-red-500 text-xs mt-1">{errors.reason}</p>}
              </div>
            </div>
          </div>

          {/* ── Right: customer, totals, payments ── */}
          <div className="lg:col-span-2 space-y-4">
            <div className="card p-4 space-y-3">
              <h2 className="font-semibold text-surface-900">Customer <span className="text-surface-400 font-normal text-sm">(optional)</span></h2>
              <input className="input" placeholder="Name" maxLength={100} value={customer.name} aria-label="Customer name"
                onChange={e => setCustomer(c => ({ ...c, name: e.target.value }))} />
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <input className={`input ${errors.phone ? 'border-red-400' : ''}`} type="tel" placeholder="Phone" aria-label="Customer phone"
                    value={customer.phone} onChange={e => setCustomer(c => ({ ...c, phone: e.target.value }))} />
                  {errors.phone && <p className="text-red-500 text-xs mt-1">{errors.phone}</p>}
                </div>
                <div>
                  <input className={`input ${errors.email ? 'border-red-400' : ''}`} type="email" placeholder="Email" aria-label="Customer email"
                    value={customer.email} onChange={e => setCustomer(c => ({ ...c, email: e.target.value }))} />
                  {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email}</p>}
                </div>
              </div>
            </div>

            <div className="card p-4 space-y-2 text-sm">
              <div className="flex justify-between text-surface-500"><span>Subtotal</span><span>{naira(subTotal)}</span></div>
              {discountNum > 0 && <div className="flex justify-between text-emerald-600"><span>Discount</span><span>–{naira(discountNum)}</span></div>}
              <div className="flex justify-between text-surface-500"><span>VAT (7.5%)</span><span>{naira(tax)}</span></div>
              <div className="flex justify-between font-bold text-lg pt-2 border-t border-surface-100"><span>Total</span><span>{naira(total)}</span></div>
            </div>

            <div className="card p-4 space-y-3">
              <h2 className="font-semibold text-surface-900">Payment</h2>
              {payments.map(p => (
                <div key={p.key} className="space-y-2 pb-3 border-b border-surface-100 last:border-0 last:pb-0">
                  <div className="flex gap-2">
                    <select className="input flex-1 text-sm" value={p.method} aria-label="Payment method"
                      onChange={e => updatePayment(p.key, { method: e.target.value })}>
                      {METHODS.map(m => <option key={m.id} value={m.id}>{m.label}</option>)}
                    </select>
                    <input type="number" min="0" step="0.01" className="input w-32 text-sm" placeholder="Amount" aria-label="Amount"
                      value={p.amount} onChange={e => updatePayment(p.key, { amount: e.target.value })} />
                    {payments.length > 1 && (
                      <button type="button" className="btn-icon text-red-400 hover:bg-red-50" aria-label="Remove payment"
                        onClick={() => setPayments(prev => prev.filter(x => x.key !== p.key))}><Trash2 className="w-3.5 h-3.5" /></button>
                    )}
                  </div>
                  {NEEDS_REFERENCE.includes(p.method) && (
                    <input className="input text-sm" placeholder="Reference (terminal or transfer ref)" maxLength={100} aria-label="Reference"
                      value={p.reference} onChange={e => updatePayment(p.key, { reference: e.target.value })} />
                  )}
                  <div className="flex items-center justify-between">
                    {errors[`pay${p.key}`] ? <p className="text-red-500 text-xs">{errors[`pay${p.key}`]}</p> : <span />}
                    {remaining > 0 && (
                      <button type="button" className="text-xs text-brand-600 hover:underline"
                        onClick={() => updatePayment(p.key, { amount: String(round2((Number(p.amount) || 0) + remaining)) })}>
                        Fill remaining {naira(remaining)}
                      </button>
                    )}
                  </div>
                </div>
              ))}
              {payments.length < 5 && (
                <button type="button" className="text-sm text-brand-600 hover:underline flex items-center gap-1"
                  onClick={() => setPayments(prev => [...prev, newPayment('Card')])}>
                  <Plus className="w-3.5 h-3.5" /> Split payment
                </button>
              )}

              <div className="pt-2 border-t border-surface-100 text-sm space-y-1">
                <div className="flex justify-between"><span className="text-surface-500">Paid</span><span>{naira(paid)}</span></div>
                {remaining > 0
                  ? <div className="flex justify-between font-semibold text-amber-600"><span>Still to pay</span><span>{naira(remaining)}</span></div>
                  : <div className="flex justify-between font-semibold text-emerald-600"><span>Change</span><span>{naira(change)}</span></div>}
              </div>
              {errors.payments && <p className="text-red-500 text-sm">{errors.payments}</p>}

              <button type="button" className="btn-primary w-full btn-lg" onClick={complete}
                disabled={submitting || lines.length === 0}>
                {submitting && !approvalOpen ? <Spinner size="sm" /> : needsApproval ? 'Complete sale (needs approval)' : 'Complete sale'}
              </button>
              <p className="text-xs text-surface-400 text-center">
                Managers set their approval PIN under <Link to="/account/profile" className="text-brand-600 hover:underline">My Account</Link>.
              </p>
            </div>
          </div>
        </div>
      </div>

      <ApprovalModal open={approvalOpen} onClose={() => setApprovalOpen(false)} submitting={submitting}
        onApprove={(approval, setApprovalError) => submitSale(approval, setApprovalError)} />
    </AdminLayout>
  )
}
