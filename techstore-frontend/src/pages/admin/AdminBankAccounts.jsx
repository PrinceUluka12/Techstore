import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Plus, Edit2, Trash2, Landmark, AlertTriangle, History } from 'lucide-react'
import toast from 'react-hot-toast'
import { format } from 'date-fns'
import { bankAccountApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, Input, Spinner, Pagination } from '../../components/ui'
import { requiredText, withChecks, showApiError } from '../../utils/validation'

// Suggestions for the bank name box; any other name can be typed
const NIGERIAN_BANKS = [
  'Access Bank', 'Citibank Nigeria', 'Ecobank Nigeria', 'Fidelity Bank', 'First Bank of Nigeria',
  'First City Monument Bank (FCMB)', 'Globus Bank', 'Guaranty Trust Bank (GTBank)', 'Keystone Bank', 'Kuda Bank',
  'Moniepoint MFB', 'OPay', 'PalmPay', 'Polaris Bank', 'Providus Bank', 'Stanbic IBTC Bank',
  'Standard Chartered Bank', 'Sterling Bank', 'SunTrust Bank', 'Titan Trust Bank', 'Union Bank of Nigeria',
  'United Bank for Africa (UBA)', 'Unity Bank', 'Wema Bank', 'Zenith Bank',
]

// Mirrors SaveBankAccountRequestValidator
const accountNumberRules = withChecks(requiredText('Account number', 20), {
  nuban: v => /^\d{10}$/.test(v.replace(/\s/g, '')) || 'Enter the 10-digit account number',
})

const EMPTY_ACCOUNT = { bankName: '', accountName: '', accountNumber: '', isActive: true }
const ACTION_BADGES = { Added: 'badge-green', Changed: 'badge-blue', Removed: 'badge-red' }
const HISTORY_PAGE_SIZE = 20

function AccountForm({ account, onSaved, onClose }) {
  const isEdit = !!account
  const { register, handleSubmit, setError, formState: { errors } } = useForm({
    mode: 'onTouched',
    defaultValues: isEdit ? { ...EMPTY_ACCOUNT, ...account } : EMPTY_ACCOUNT,
  })
  const [saving, setSaving] = useState(false)

  const onSubmit = async (data) => {
    setSaving(true)
    const payload = {
      bankName:      data.bankName.trim(),
      accountName:   data.accountName.trim(),
      accountNumber: data.accountNumber.replace(/\s/g, ''),
      isActive:      !!data.isActive,
    }
    try {
      if (isEdit) await bankAccountApi.update(account.id, payload)
      else        await bankAccountApi.create(payload)
      toast.success(isEdit ? 'Account updated' : 'Account added')
      onSaved()
    } catch (err) {
      showApiError(err, setError, 'Failed to save the account')
    } finally { setSaving(false) }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
      <Input label="Bank" placeholder="e.g. Zenith Bank" list="nigerian-banks" autoComplete="off"
        error={errors.bankName?.message} {...register('bankName', requiredText('Bank', 100))} />
      <datalist id="nigerian-banks">
        {NIGERIAN_BANKS.map(b => <option key={b} value={b} />)}
      </datalist>
      <Input label="Account Name" placeholder="As it appears on the account"
        error={errors.accountName?.message} {...register('accountName', requiredText('Account name', 150))} />
      <Input label="Account Number" placeholder="10 digits" inputMode="numeric" maxLength={13} className="font-mono"
        error={errors.accountNumber?.message} {...register('accountNumber', accountNumberRules)} />

      <label className="flex items-start gap-3 cursor-pointer">
        <input type="checkbox" className="w-4 h-4 mt-0.5 accent-brand-500" {...register('isActive')} />
        <span className="text-sm">
          <span className="font-medium text-surface-800">Show at checkout</span>
          <span className="block text-xs text-surface-400">Customers see it when they order online and on their unpaid orders.</span>
        </span>
      </label>

      <p className="text-xs text-surface-400">Double-check the number: customers send money to exactly what's shown. The change is recorded in the history.</p>

      <div className="flex justify-end gap-3 pt-2">
        <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
        <button type="submit" disabled={saving} className="btn-primary">
          {saving ? <Spinner size="sm" /> : isEdit ? 'Save Changes' : 'Add Account'}
        </button>
      </div>
    </form>
  )
}

const when = (d) => format(new Date(d.endsWith('Z') ? d : `${d}Z`), 'd MMM yyyy, HH:mm')

export default function AdminBankAccounts() {
  const [accounts, setAccounts] = useState([])
  const [loading, setLoading]   = useState(true)
  const [editing, setEditing]   = useState(null) // null | 'new' | account
  const [busyId, setBusyId]     = useState(null)
  const [history, setHistory]   = useState({ items: [], total: 0 })
  const [page, setPage]         = useState(1)

  const loadAccounts = () => {
    setLoading(true)
    bankAccountApi.all()
      .then(r => setAccounts(r.data))
      .catch(err => showApiError(err, null, 'Failed to load bank accounts'))
      .finally(() => setLoading(false))
  }
  const loadHistory = (p = page) =>
    bankAccountApi.history({ page: p, pageSize: HISTORY_PAGE_SIZE })
      .then(r => setHistory(r.data))
      .catch(() => toast.error('Failed to load the history'))

  useEffect(loadAccounts, [])
  useEffect(() => { loadHistory(page) }, [page])

  // Any change shows at the top of the history
  const changed = () => { loadAccounts(); setPage(1); loadHistory(1) }
  const onSaved = () => { setEditing(null); changed() }

  const toggle = async (a) => {
    setBusyId(a.id)
    try {
      await bankAccountApi.update(a.id, { ...a, isActive: !a.isActive })
      toast.success(a.isActive ? 'Hidden from checkout' : 'Shown at checkout')
      changed()
    } catch (err) { showApiError(err, null, 'Failed to update the account') }
    finally { setBusyId(null) }
  }

  const remove = async (a) => {
    if (!confirm(`Remove ${a.bankName} ${a.accountNumber}? Customers won't see it any more. This is recorded in the history.`)) return
    setBusyId(a.id)
    try {
      await bankAccountApi.delete(a.id)
      toast.success('Account removed')
      changed()
    } catch (err) { showApiError(err, null, 'Failed to remove the account') }
    finally { setBusyId(null) }
  }

  const noneShown = !loading && !accounts.some(a => a.isActive)

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1100px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Bank Accounts</h1>
            <p className="text-surface-400 text-sm mt-1">Where customers pay for online orders. Every change is recorded in the history below.</p>
          </div>
          <button onClick={() => setEditing('new')} className="btn-primary flex items-center gap-2">
            <Plus className="w-4 h-4" /> Add Account
          </button>
        </div>

        {noneShown && (
          <div className="flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
            <AlertTriangle className="w-4 h-4 mt-0.5 flex-shrink-0" />
            <span><strong>Online orders are paused.</strong> No account is shown at checkout, so customers can't place orders until one is.</span>
          </div>
        )}

        <div className="card overflow-hidden">
          {loading ? (
            <div className="flex items-center justify-center py-16"><Spinner /></div>
          ) : accounts.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 text-surface-400 gap-3">
              <Landmark className="w-8 h-8" />
              <p className="text-sm">No bank accounts yet.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-surface-100 bg-surface-50 text-left text-xs text-surface-400 uppercase tracking-wide">
                    <th className="px-4 py-3">Bank</th>
                    <th className="px-4 py-3">Account Number</th>
                    <th className="px-4 py-3">Account Name</th>
                    <th className="px-4 py-3">At Checkout</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-50">
                  {accounts.map(a => (
                    <tr key={a.id} className="hover:bg-surface-50 transition-colors">
                      <td className="px-4 py-3 font-medium text-surface-900">{a.bankName}</td>
                      <td className="px-4 py-3 font-mono text-surface-800 tracking-wide">{a.accountNumber}</td>
                      <td className="px-4 py-3 text-surface-600">{a.accountName}</td>
                      <td className="px-4 py-3">
                        <button type="button" onClick={() => toggle(a)} disabled={busyId === a.id}
                          title={a.isActive ? 'Hide from checkout' : 'Show at checkout'}
                          className={`badge cursor-pointer ${a.isActive ? 'badge-green' : 'badge-gray'}`}>
                          {a.isActive ? 'Shown' : 'Hidden'}
                        </button>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <button onClick={() => setEditing(a)} disabled={busyId === a.id}
                            className="btn-icon text-surface-400 hover:text-brand-600" title="Edit account">
                            <Edit2 className="w-3.5 h-3.5" />
                          </button>
                          <button onClick={() => remove(a)} disabled={busyId === a.id}
                            className="btn-icon text-red-400 hover:text-red-600 hover:bg-red-50" title="Remove account">
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="card overflow-hidden">
          <div className="flex items-center gap-2 px-4 py-3 border-b border-surface-100">
            <History className="w-4 h-4 text-surface-400" />
            <h2 className="font-semibold text-surface-900">History</h2>
            <span className="text-xs text-surface-400">· every change, newest first</span>
          </div>
          {history.items.length === 0 ? (
            <p className="px-4 py-8 text-sm text-surface-400 text-center">No changes yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-surface-100 bg-surface-50 text-left text-xs text-surface-400 uppercase tracking-wide">
                    <th className="px-4 py-3 whitespace-nowrap">When</th>
                    <th className="px-4 py-3">Who</th>
                    <th className="px-4 py-3">What</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-50">
                  {history.items.map(l => (
                    <tr key={l.id}>
                      <td className="px-4 py-3 text-surface-500 whitespace-nowrap">{when(l.changedAt)}</td>
                      <td className="px-4 py-3">
                        <div className="text-surface-800">{l.changedByName}</div>
                        <div className="text-xs text-surface-400">{l.changedByEmail}</div>
                      </td>
                      <td className="px-4 py-3">
                        <span className={`badge ${ACTION_BADGES[l.action] ?? 'badge-gray'} mr-2`}>{l.action}</span>
                        <span className="text-surface-700">{l.details}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
        <Pagination page={page} totalPages={Math.ceil(history.total / HISTORY_PAGE_SIZE)} onChange={setPage} />
      </div>

      <Modal open={!!editing} onClose={() => setEditing(null)} title={editing === 'new' ? 'Add Bank Account' : 'Edit Bank Account'}>
        {editing && (
          <AccountForm account={editing === 'new' ? null : editing} onSaved={onSaved} onClose={() => setEditing(null)} />
        )}
      </Modal>
    </AdminLayout>
  )
}
