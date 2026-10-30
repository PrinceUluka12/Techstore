import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Plus, Edit2, Store, Globe } from 'lucide-react'
import toast from 'react-hot-toast'
import { storeApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, Input, Spinner } from '../../components/ui'
import { requiredText, maxText, withChecks, showApiError } from '../../utils/validation'

// Mirrors CreateStoreRequestValidator
const codeRules = withChecks(requiredText('Code', 10), {
  format: v => /^[A-Za-z0-9]{2,10}$/.test(v.trim()) || '2–10 letters or numbers, e.g. IKJ',
})
const phoneRules = {
  validate: v => !v?.trim() || /^[0-9+()\-\s]{7,20}$/.test(v.trim()) || 'Enter a valid phone number',
}

const EMPTY_STORE = { name: '', code: '', address: '', city: '', phone: '', sendsOnlineOrders: false, isActive: true }

function StoreForm({ store, onSaved, onClose }) {
  const isEdit = !!store
  const { register, handleSubmit, setError, formState: { errors } } = useForm({
    mode: 'onTouched',
    defaultValues: isEdit
      ? { ...EMPTY_STORE, ...store, address: store.address ?? '', city: store.city ?? '', phone: store.phone ?? '' }
      : EMPTY_STORE,
  })
  const [saving, setSaving] = useState(false)
  // The API won't let the shop that sends online orders be switched off or deactivated
  const lockedOnline = isEdit && store.sendsOnlineOrders

  const onSubmit = async (data) => {
    setSaving(true)
    const payload = {
      name:    data.name.trim(),
      code:    data.code.trim().toUpperCase(),
      address: data.address.trim(),
      city:    data.city.trim(),
      phone:   data.phone.trim(),
      sendsOnlineOrders: !!data.sendsOnlineOrders,
    }
    try {
      if (isEdit) await storeApi.update(store.id, { ...payload, isActive: !!data.isActive })
      else        await storeApi.create(payload)
      toast.success(isEdit ? 'Shop updated' : 'Shop created')
      onSaved()
    } catch (err) {
      showApiError(err, setError, 'Failed to save shop')
    } finally { setSaving(false) }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <Input label="Shop Name" className="sm:col-span-2" placeholder="e.g. Ikeja City Mall"
          error={errors.name?.message} {...register('name', requiredText('Shop name', 100))} />
        <Input label="Code" placeholder="IKJ" style={{ textTransform: 'uppercase' }}
          error={errors.code?.message} {...register('code', codeRules)} />
      </div>
      <Input label="Address" placeholder="Street address (printed on receipts)"
        error={errors.address?.message} {...register('address', maxText(200))} />
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Input label="City" error={errors.city?.message} {...register('city', maxText(100))} />
        <Input label="Phone" type="tel" placeholder="08012345678"
          error={errors.phone?.message} {...register('phone', phoneRules)} />
      </div>

      <label className="flex items-start gap-3 cursor-pointer">
        <input type="checkbox" className="w-4 h-4 mt-0.5 accent-brand-500" disabled={lockedOnline}
          {...register('sendsOnlineOrders')} />
        <span className="text-sm">
          <span className="font-medium text-surface-800">Sends online orders</span>
          <span className="block text-xs text-surface-400">
            {lockedOnline
              ? 'To change this, turn it on for another shop instead.'
              : 'The website sells this shop’s stock. Only one shop can do this, so turning it on here turns it off elsewhere.'}
          </span>
        </span>
      </label>

      {isEdit && (
        <label className="flex items-center gap-3 cursor-pointer">
          <input type="checkbox" className="w-4 h-4 accent-brand-500" disabled={lockedOnline}
            {...register('isActive')} />
          <span className="text-sm text-surface-700">Active</span>
        </label>
      )}

      <div className="flex justify-end gap-3 pt-2">
        <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
        <button type="submit" disabled={saving} className="btn-primary">
          {saving ? <Spinner size="sm" /> : isEdit ? 'Save Changes' : 'Create Shop'}
        </button>
      </div>
    </form>
  )
}

export default function AdminStores() {
  const [stores, setStores]   = useState([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState(null) // null | 'new' | store

  const load = () => {
    setLoading(true)
    storeApi.getAll()
      .then(r => setStores(r.data))
      .catch(() => toast.error('Failed to load shops'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const onSaved = () => { setEditing(null); load() }

  return (
    <AdminLayout>
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="font-display text-2xl font-semibold">Shops</h1>
            <p className="text-surface-400 text-sm mt-1">Each shop keeps its own stock</p>
          </div>
          <button onClick={() => setEditing('new')} className="btn-primary flex items-center gap-2">
            <Plus className="w-4 h-4" /> New Shop
          </button>
        </div>

        <div className="card overflow-hidden">
          {loading ? (
            <div className="flex items-center justify-center py-20"><Spinner /></div>
          ) : stores.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-20 text-surface-400 gap-3">
              <Store className="w-8 h-8" />
              <p className="text-sm">No shops yet.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-surface-100 bg-surface-50 text-left text-xs text-surface-400 uppercase tracking-wide">
                    <th className="px-4 py-3">Shop</th>
                    <th className="px-4 py-3">Code</th>
                    <th className="px-4 py-3">Address</th>
                    <th className="px-4 py-3">Phone</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-50">
                  {stores.map(s => (
                    <tr key={s.id} className="hover:bg-surface-50 transition-colors">
                      <td className="px-4 py-3">
                        <div className="font-medium text-surface-900">{s.name}</div>
                        {s.sendsOnlineOrders && (
                          <span className="inline-flex items-center gap-1 text-xs text-brand-600 mt-0.5">
                            <Globe className="w-3 h-3" /> Sends online orders
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 font-mono text-surface-600">{s.code}</td>
                      <td className="px-4 py-3 text-surface-500">
                        {[s.address, s.city].filter(Boolean).join(', ') || '—'}
                      </td>
                      <td className="px-4 py-3 text-surface-500">{s.phone || '—'}</td>
                      <td className="px-4 py-3">
                        <span className={`badge ${s.isActive ? 'badge-green' : 'badge-gray'}`}>
                          {s.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <button onClick={() => setEditing(s)} className="btn-icon text-surface-400 hover:text-brand-600" title="Edit shop">
                          <Edit2 className="w-3.5 h-3.5" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <Modal open={!!editing} onClose={() => setEditing(null)} title={editing === 'new' ? 'New Shop' : 'Edit Shop'}>
        {editing && (
          <StoreForm store={editing === 'new' ? null : editing} onSaved={onSaved} onClose={() => setEditing(null)} />
        )}
      </Modal>
    </AdminLayout>
  )
}
