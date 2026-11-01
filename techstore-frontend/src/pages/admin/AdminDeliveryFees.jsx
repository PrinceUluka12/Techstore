import { useEffect, useState } from 'react'
import { Truck, AlertTriangle } from 'lucide-react'
import toast from 'react-hot-toast'
import { format } from 'date-fns'
import { deliveryApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Spinner } from '../../components/ui'
import { showApiError } from '../../utils/validation'

// Same limit as DeliveryRateService
const MAX_FEE = 1_000_000
const toInput = fee => fee == null ? '' : String(fee)
const problem = v => v === '' ? null
  : !/^\d+(\.\d{1,2})?$/.test(v) ? 'Numbers only'
  : Number(v) > MAX_FEE ? 'Too high'
  : null

// Delivery fee for each state. A blank fee means no delivery there (customers can still pick
// up from a shop); 0 means free delivery.
export default function AdminDeliveryFees() {
  const [rates, setRates]     = useState([]) // as saved: [{ state, fee, updatedByName, updatedAt }]
  const [values, setValues]   = useState({}) // being edited: state -> text
  const [loading, setLoading] = useState(true)
  const [saving, setSaving]   = useState(false)
  const [allFee, setAllFee]   = useState('')

  const show = (data) => {
    setRates(data)
    setValues(Object.fromEntries(data.map(r => [r.state, toInput(r.fee)])))
  }

  useEffect(() => {
    deliveryApi.all()
      .then(r => show(r.data))
      .catch(err => showApiError(err, null, 'Failed to load delivery fees'))
      .finally(() => setLoading(false))
  }, [])

  const changed = rates.filter(r => (values[r.state] ?? '').trim() !== toInput(r.fee))
  const invalid = rates.some(r => problem((values[r.state] ?? '').trim()))
  const delivering = rates.filter(r => (values[r.state] ?? '').trim() !== '').length

  const save = async () => {
    setSaving(true)
    try {
      const { data } = await deliveryApi.save(changed.map(r => {
        const v = values[r.state].trim()
        return { state: r.state, fee: v === '' ? null : Number(v) }
      }))
      show(data)
      toast.success('Delivery fees saved')
    } catch (err) { showApiError(err, null, 'Failed to save delivery fees') }
    finally { setSaving(false) }
  }

  const applyToAll = () => {
    if (problem(allFee.trim()) || allFee.trim() === '') return
    setValues(Object.fromEntries(rates.map(r => [r.state, allFee.trim()])))
  }

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[900px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Delivery Fees</h1>
            <p className="text-surface-400 text-sm mt-1">
              What customers pay for delivery to each state. Leave a state blank to offer pickup only; 0 is free delivery.
            </p>
          </div>
          <button onClick={save} disabled={saving || loading || !changed.length || invalid} className="btn-primary flex items-center gap-2">
            {saving ? <Spinner size="sm" /> : <Truck className="w-4 h-4" />}
            Save{changed.length ? ` ${changed.length} change${changed.length === 1 ? '' : 's'}` : ''}
          </button>
        </div>

        {!loading && delivering === 0 && (
          <div className="flex items-start gap-3 bg-amber-50 border border-amber-200 rounded-xl p-4 text-sm text-amber-800">
            <AlertTriangle className="w-4 h-4 flex-shrink-0 mt-0.5" />
            <p>No state has a delivery fee, so customers can only choose pickup from a shop.</p>
          </div>
        )}

        <div className="card p-4 flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[180px]">
            <label className="label" htmlFor="all-fee">Same fee for every state</label>
            <input id="all-fee" className="input" inputMode="decimal" placeholder="e.g. 5000"
              value={allFee} onChange={e => setAllFee(e.target.value)} />
          </div>
          <button type="button" onClick={applyToAll} className="btn-secondary"
            disabled={allFee.trim() === '' || !!problem(allFee.trim())}>
            Fill in all states
          </button>
          <p className="text-xs text-surface-400 w-full">Fills the boxes below; nothing is saved until you press Save.</p>
        </div>

        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr><th>State</th><th>Delivery fee (₦)</th><th>Last changed</th></tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={3} className="text-center py-12"><Spinner /></td></tr>
              ) : rates.map(r => {
                const v = values[r.state] ?? ''
                const error = problem(v.trim())
                return (
                  <tr key={r.state}>
                    <td className="font-medium text-surface-900">{r.state}</td>
                    <td>
                      <input className={`input text-sm w-36 ${error ? 'border-red-400' : ''}`} inputMode="decimal"
                        aria-label={`Delivery fee for ${r.state}`} placeholder="Pickup only"
                        value={v} onChange={e => setValues(vs => ({ ...vs, [r.state]: e.target.value }))} />
                      {error && <p className="text-xs text-red-500 mt-1">{error}</p>}
                    </td>
                    <td className="text-xs text-surface-400">
                      {r.updatedAt ? `${r.updatedByName ?? 'Someone'} · ${format(new Date(r.updatedAt), 'MMM d, yyyy')}` : '—'}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        {!loading && <p className="text-xs text-surface-400">Delivering to {delivering} of {rates.length} states.</p>}
      </div>
    </AdminLayout>
  )
}
