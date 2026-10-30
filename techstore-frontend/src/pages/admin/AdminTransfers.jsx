import { useEffect, useState } from 'react'
import { format } from 'date-fns'
import { ArrowRight, Plus, PackageCheck, Undo2, Trash2, Search, ArrowLeftRight } from 'lucide-react'
import toast from 'react-hot-toast'
import { authApi, storeApi, transferApi } from '../../services/api'
import { useAuthStore } from '../../store'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, Spinner } from '../../components/ui'
import { showApiError } from '../../utils/validation'

const STATUS_TABS = [
  { id: 'InTransit', label: 'In transit' },
  { id: 'Received',  label: 'Received' },
  { id: 'Cancelled', label: 'Cancelled' },
  { id: '',          label: 'All' },
]
const STATUS_BADGE = { InTransit: 'badge-yellow', Received: 'badge-green', Cancelled: 'badge-gray' }
const STATUS_LABEL = { InTransit: 'In transit', Received: 'Received', Cancelled: 'Cancelled' }

// API dates are UTC without a "Z"
const when = (d) => d ? format(new Date(d.endsWith('Z') ? d : `${d}Z`), 'd MMM yyyy, HH:mm') : '—'

// ── New transfer ──────────────────────────────────────────────────────────────
function NewTransferModal({ open, onClose, stores, admin, homeStoreId, onCreated }) {
  const [fromId, setFromId] = useState(null)
  const [toId, setToId]     = useState('')
  const [query, setQuery]   = useState('')
  const [results, setResults] = useState([])
  const [lines, setLines]   = useState([])
  const [notes, setNotes]   = useState('')
  const [error, setError]   = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!open) return
    setFromId(admin ? (homeStoreId ?? stores[0]?.id ?? null) : homeStoreId)
    setToId(''); setQuery(''); setLines([]); setNotes(''); setError('')
  }, [open])

  // Products with their free stock at the sending shop
  useEffect(() => {
    if (!open || !fromId) return
    const t = setTimeout(() => {
      transferApi.products(query.trim(), fromId).then(r => setResults(r.data)).catch(err => showApiError(err, null, 'Search failed'))
    }, 250)
    return () => clearTimeout(t)
  }, [query, fromId, open])

  const add = (p) => {
    if (p.quantityAvailable <= 0) return toast.error(`No free ${p.name} at this shop`)
    setLines(prev => prev.some(l => l.product.id === p.id)
      ? prev.map(l => l.product.id === p.id ? { ...l, quantity: Math.min(l.quantity + 1, p.quantityAvailable) } : l)
      : [...prev, { product: p, quantity: 1 }])
  }

  const submit = async (e) => {
    e.preventDefault()
    if (!toId) return setError('Choose the shop to send to')
    if (lines.length === 0) return setError('Add at least one product')
    const bad = lines.find(l => !(l.quantity >= 1 && l.quantity <= l.product.quantityAvailable))
    if (bad) return setError(`${bad.product.name}: between 1 and ${bad.product.quantityAvailable}`)
    setError(''); setSaving(true)
    try {
      const { data } = await transferApi.create({
        fromStoreId: fromId,
        toStoreId: Number(toId),
        items: lines.map(l => ({ productId: l.product.id, quantity: l.quantity })),
        notes: notes.trim() || null,
      })
      toast.success(`Transfer ${data.transferNumber} sent`)
      onCreated()
    } catch (err) {
      showApiError(err, null, 'Transfer failed')
    } finally { setSaving(false) }
  }

  const fromShop = stores.find(s => s.id === fromId)

  return (
    <Modal open={open} onClose={onClose} title="New stock transfer" size="lg">
      <form onSubmit={submit} noValidate className="space-y-4">
        <div className="grid sm:grid-cols-2 gap-4">
          <div>
            <label className="label">From</label>
            {admin ? (
              <select className="input" value={fromId ?? ''} disabled={lines.length > 0} aria-label="From shop"
                title={lines.length > 0 ? 'Remove the products to change the sending shop' : undefined}
                onChange={e => { setFromId(Number(e.target.value)); setToId('') }}>
                {stores.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            ) : <p className="input bg-surface-50">{fromShop?.name ?? 'Your shop'}</p>}
          </div>
          <div>
            <label className="label">To</label>
            <select className="input" value={toId} onChange={e => setToId(e.target.value)} aria-label="To shop">
              <option value="">Choose a shop…</option>
              {stores.filter(s => s.id !== fromId).map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </div>
        </div>

        <div>
          <label className="label">Products <span className="text-surface-400 font-normal">(free stock at {fromShop?.name})</span></label>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400" />
            <input className="input pl-9" placeholder="Search by name or SKU…" value={query} aria-label="Search products"
              onChange={e => setQuery(e.target.value)} />
          </div>
          <div className="mt-2 max-h-40 overflow-y-auto divide-y divide-surface-50 border border-surface-100 rounded-xl">
            {results.length === 0 ? <p className="text-sm text-surface-400 text-center py-4">No products found</p>
              : results.map(p => (
                <button key={p.id} type="button" onClick={() => add(p)} disabled={p.quantityAvailable <= 0}
                  className="w-full flex items-center justify-between px-3 py-2 text-sm hover:bg-surface-50 disabled:opacity-50">
                  <span className="text-left"><span className="font-medium">{p.name}</span> <span className="text-xs text-surface-400 font-mono">{p.sku}</span></span>
                  <span className="text-xs text-surface-500">{p.quantityAvailable} free</span>
                </button>
              ))}
          </div>
        </div>

        {lines.length > 0 && (
          <div className="space-y-2">
            {lines.map(l => (
              <div key={l.product.id} className="flex items-center gap-3 text-sm">
                <span className="flex-1 truncate">{l.product.name}</span>
                <input type="number" min="1" max={l.product.quantityAvailable} step="1" className="input w-24 py-1.5"
                  aria-label={`Quantity of ${l.product.name}`} value={l.quantity}
                  onChange={e => setLines(prev => prev.map(x => x === l ? { ...x, quantity: Number(e.target.value) } : x))} />
                <span className="text-xs text-surface-400 w-16">of {l.product.quantityAvailable}</span>
                <button type="button" className="btn-icon text-red-400 hover:bg-red-50" aria-label={`Remove ${l.product.name}`}
                  onClick={() => setLines(prev => prev.filter(x => x !== l))}><Trash2 className="w-3.5 h-3.5" /></button>
              </div>
            ))}
          </div>
        )}

        <div>
          <label className="label">Notes <span className="text-surface-400 font-normal">(optional)</span></label>
          <input className="input" maxLength={500} placeholder="e.g. Driver name, waybill number" value={notes}
            onChange={e => setNotes(e.target.value)} />
        </div>

        {error && <p className="text-red-500 text-sm">{error}</p>}
        <div className="flex justify-end gap-3">
          <button type="button" className="btn-secondary" onClick={onClose}>Cancel</button>
          <button type="submit" className="btn-primary" disabled={saving}>{saving ? <Spinner size="sm" /> : 'Send stock'}</button>
        </div>
      </form>
    </Modal>
  )
}

// ── Transfers page ────────────────────────────────────────────────────────────
export default function AdminTransfers() {
  const { isAdmin } = useAuthStore()
  const admin = isAdmin()
  const [stores, setStores] = useState([])
  const [homeStoreId, setHomeStoreId] = useState(null)
  const [status, setStatus] = useState('InTransit')
  const [shopFilter, setShopFilter] = useState('')
  const [transfers, setTransfers] = useState([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState(null)
  const [newOpen, setNewOpen] = useState(false)

  useEffect(() => {
    Promise.all([authApi.getMe(), storeApi.getAll()]).then(([me, s]) => {
      setHomeStoreId(me.data.storeId ?? null)
      setStores(s.data.filter(x => x.isActive))
    }).catch(() => toast.error('Failed to load shops'))
  }, [])

  const load = () => {
    setLoading(true)
    transferApi.list({ status: status || undefined, storeId: shopFilter || undefined })
      .then(r => setTransfers(r.data))
      .catch(err => showApiError(err, null, 'Failed to load transfers'))
      .finally(() => setLoading(false))
  }
  useEffect(load, [status, shopFilter])

  const act = async (t, action) => {
    if (action === 'cancel' && !confirm(`Cancel ${t.transferNumber}? The stock goes back to ${t.fromStore.name}.`)) return
    setBusyId(t.id)
    try {
      await (action === 'receive' ? transferApi.receive(t.id) : transferApi.cancel(t.id))
      toast.success(action === 'receive' ? `Stock added to ${t.toStore.name}` : `Stock returned to ${t.fromStore.name}`)
      load()
    } catch (err) {
      showApiError(err, null, 'Could not update the transfer')
    } finally { setBusyId(null) }
  }

  const canSend = admin || homeStoreId != null

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Stock Transfers</h1>
            <p className="text-surface-400 text-sm mt-0.5">Stock leaves the sending shop when sent and arrives when the other shop confirms it</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {admin && (
              <select className="input w-auto text-sm" value={shopFilter} onChange={e => setShopFilter(e.target.value)} aria-label="Filter by shop">
                <option value="">All shops</option>
                {stores.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            )}
            <button type="button" className="btn-primary gap-2" onClick={() => setNewOpen(true)} disabled={!canSend || stores.length < 2}
              title={!canSend ? 'You need a home shop to send stock' : stores.length < 2 ? 'Add a second shop first' : undefined}>
              <Plus className="w-4 h-4" /> New transfer
            </button>
          </div>
        </div>

        <div className="flex gap-1 border-b border-surface-100">
          {STATUS_TABS.map(tab => (
            <button key={tab.id} type="button" onClick={() => setStatus(tab.id)}
              className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${status === tab.id ? 'border-brand-500 text-brand-600' : 'border-transparent text-surface-500 hover:text-surface-800'}`}>
              {tab.label}
            </button>
          ))}
        </div>

        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Transfer</th>
                <th>Route</th>
                <th>Items</th>
                <th>Status</th>
                <th>Sent</th>
                <th>Received / cancelled</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="text-center py-12"><Spinner /></td></tr>
              ) : transfers.length === 0 ? (
                <tr><td colSpan={7} className="text-center py-12 text-surface-400">
                  <ArrowLeftRight className="w-6 h-6 mx-auto mb-2 text-surface-300" />No transfers
                </td></tr>
              ) : transfers.map(t => {
                const canReceive = t.status === 'InTransit' && (admin || homeStoreId === t.toStore.id)
                const canCancel  = t.status === 'InTransit' && (admin || homeStoreId === t.fromStore.id)
                return (
                  <tr key={t.id}>
                    <td>
                      <span className="font-mono text-xs text-brand-600 font-medium">{t.transferNumber}</span>
                      {t.notes && <p className="text-xs text-surface-400 truncate max-w-[200px]">{t.notes}</p>}
                    </td>
                    <td>
                      <span className="flex items-center gap-1.5 text-sm">
                        {t.fromStore.name} <ArrowRight className="w-3.5 h-3.5 text-surface-400" /> {t.toStore.name}
                      </span>
                    </td>
                    <td>
                      <p className="text-sm font-medium">{t.totalUnits} unit{t.totalUnits === 1 ? '' : 's'}</p>
                      <p className="text-xs text-surface-400 truncate max-w-[220px]">
                        {t.items.map(i => `${i.quantity}× ${i.productName}`).join(', ')}
                      </p>
                    </td>
                    <td><span className={`badge ${STATUS_BADGE[t.status]}`}>{STATUS_LABEL[t.status]}</span></td>
                    <td><p className="text-xs">{t.createdBy}</p><p className="text-xs text-surface-400">{when(t.createdAt)}</p></td>
                    <td>{t.completedAt
                      ? <><p className="text-xs">{t.completedBy}</p><p className="text-xs text-surface-400">{when(t.completedAt)}</p></>
                      : <span className="text-xs text-surface-300">—</span>}</td>
                    <td>
                      <div className="flex items-center gap-2">
                        {canReceive && (
                          <button type="button" className="btn-secondary btn-sm gap-1.5 text-emerald-700" disabled={busyId === t.id}
                            onClick={() => act(t, 'receive')}>
                            <PackageCheck className="w-3.5 h-3.5" /> Receive
                          </button>
                        )}
                        {canCancel && (
                          <button type="button" className="btn-secondary btn-sm gap-1.5 text-red-600" title="Cancel and return the stock"
                            disabled={busyId === t.id} onClick={() => act(t, 'cancel')}>
                            <Undo2 className="w-3.5 h-3.5" /> Cancel
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </div>

      <NewTransferModal open={newOpen} onClose={() => setNewOpen(false)} stores={stores} admin={admin}
        homeStoreId={homeStoreId} onCreated={() => { setNewOpen(false); setStatus('InTransit'); load() }} />
    </AdminLayout>
  )
}
