import { useEffect, useState } from 'react'
import { Plus, Edit2, Trash2, Tag, ToggleLeft, ToggleRight, X } from 'lucide-react'
import { couponApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, Input, Spinner, Pagination } from '../../components/ui'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const EMPTY_FORM = {
  code: '', description: '', type: 'Percentage', value: '',
  minimumOrderAmount: '', maximumDiscountAmount: '',
  usageLimit: '', validFrom: '', validTo: '', isActive: true,
}

function toFormDate(iso) {
  if (!iso) return ''
  return iso.substring(0, 10)
}

function CouponForm({ initial, onSave, onClose, saving }) {
  const { register, handleSubmit, watch, formState: { errors } } = useForm({
    defaultValues: initial ?? EMPTY_FORM,
  })
  const type = watch('type')

  return (
    <form onSubmit={handleSubmit(onSave)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <Input
          label="Code"
          className="font-mono uppercase col-span-2"
          placeholder="SAVE10"
          error={errors.code?.message}
          {...register('code', { required: 'Required' })}
        />
        <div className="col-span-2">
          <label className="label">Description</label>
          <input className="input w-full" placeholder="Optional description…"
            {...register('description')} />
        </div>

        <div>
          <label className="label">Discount Type</label>
          <select className="input w-full" {...register('type', { required: true })}>
            <option value="Percentage">Percentage (%)</option>
            <option value="FixedAmount">Fixed Amount (₦)</option>
          </select>
        </div>

        <Input
          label={type === 'Percentage' ? 'Value (%)' : 'Value (₦)'}
          type="number" step="0.01" min="0.01"
          placeholder={type === 'Percentage' ? '10' : '500'}
          error={errors.value?.message}
          {...register('value', {
            required: 'Required',
            min: { value: 0.01, message: 'Must be > 0' },
            ...(type === 'Percentage' ? { max: { value: 100, message: 'Max 100%' } } : {}),
          })}
        />

        <Input label="Min Order Amount (₦)" type="number" step="0.01" min="0"
          placeholder="Optional" {...register('minimumOrderAmount')} />

        <Input label="Max Discount Cap (₦)" type="number" step="0.01" min="0"
          placeholder="Optional" {...register('maximumDiscountAmount')} />

        <Input label="Usage Limit" type="number" min="1"
          placeholder="Unlimited" {...register('usageLimit')} />

        <div />

        <div>
          <label className="label">Valid From</label>
          <input type="date" className="input w-full" {...register('validFrom')} />
        </div>
        <div>
          <label className="label">Valid To</label>
          <input type="date" className="input w-full" {...register('validTo')} />
        </div>

        <div className="col-span-2 flex items-center gap-3">
          <input type="checkbox" id="isActive" className="w-4 h-4 accent-brand-500"
            {...register('isActive')} />
          <label htmlFor="isActive" className="text-sm text-surface-700">Active</label>
        </div>
      </div>

      <div className="flex justify-end gap-3 pt-2">
        <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
        <button type="submit" disabled={saving} className="btn-primary flex items-center gap-2">
          {saving ? <Spinner size="sm" /> : null}
          {initial ? 'Save Changes' : 'Create Coupon'}
        </button>
      </div>
    </form>
  )
}

function DeleteModal({ coupon, onConfirm, onClose, deleting }) {
  return (
    <div className="space-y-4">
      <p className="text-surface-700 text-sm">
        Delete coupon <span className="font-mono font-semibold">{coupon.code}</span>? This cannot be undone.
      </p>
      <div className="flex justify-end gap-3">
        <button onClick={onClose} className="btn-secondary">Cancel</button>
        <button onClick={onConfirm} disabled={deleting}
          className="btn-danger flex items-center gap-2">
          {deleting ? <Spinner size="sm" /> : <Trash2 className="w-4 h-4" />}
          Delete
        </button>
      </div>
    </div>
  )
}

export default function AdminCoupons() {
  const [coupons, setCoupons]     = useState([])
  const [total, setTotal]         = useState(0)
  const [page, setPage]           = useState(1)
  const [loading, setLoading]     = useState(true)
  const [saving, setSaving]       = useState(false)
  const [deleting, setDeleting]   = useState(false)
  const [createOpen, setCreateOpen] = useState(false)
  const [editTarget, setEditTarget] = useState(null)
  const [deleteTarget, setDeleteTarget] = useState(null)
  const pageSize = 15

  const load = () => {
    setLoading(true)
    couponApi.getAll({ page, pageSize })
      .then(r => { setCoupons(r.data.items); setTotal(r.data.total) })
      .catch(() => toast.error('Failed to load coupons'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [page])

  const handleCreate = async (data) => {
    setSaving(true)
    try {
      await couponApi.create({
        code:                 data.code.toUpperCase(),
        description:          data.description || null,
        type:                 data.type,
        value:                parseFloat(data.value),
        minimumOrderAmount:   data.minimumOrderAmount   ? parseFloat(data.minimumOrderAmount)   : null,
        maximumDiscountAmount: data.maximumDiscountAmount ? parseFloat(data.maximumDiscountAmount) : null,
        usageLimit:           data.usageLimit ? parseInt(data.usageLimit) : null,
        validFrom:            data.validFrom  ? new Date(data.validFrom).toISOString() : null,
        validTo:              data.validTo    ? new Date(data.validTo).toISOString()   : null,
        isActive:             !!data.isActive,
      })
      toast.success('Coupon created')
      setCreateOpen(false)
      load()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to create coupon')
    } finally { setSaving(false) }
  }

  const handleEdit = async (data) => {
    setSaving(true)
    try {
      await couponApi.update(editTarget.id, {
        code:                 data.code.toUpperCase(),
        description:          data.description || null,
        type:                 data.type,
        value:                parseFloat(data.value),
        minimumOrderAmount:   data.minimumOrderAmount   ? parseFloat(data.minimumOrderAmount)   : null,
        maximumDiscountAmount: data.maximumDiscountAmount ? parseFloat(data.maximumDiscountAmount) : null,
        usageLimit:           data.usageLimit ? parseInt(data.usageLimit) : null,
        validFrom:            data.validFrom  ? new Date(data.validFrom).toISOString() : null,
        validTo:              data.validTo    ? new Date(data.validTo).toISOString()   : null,
        isActive:             !!data.isActive,
      })
      toast.success('Coupon updated')
      setEditTarget(null)
      load()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to update coupon')
    } finally { setSaving(false) }
  }

  const handleDelete = async () => {
    setDeleting(true)
    try {
      await couponApi.delete(deleteTarget.id)
      toast.success('Coupon deleted')
      setDeleteTarget(null)
      load()
    } catch {
      toast.error('Failed to delete coupon')
    } finally { setDeleting(false) }
  }

  const handleToggle = async (coupon) => {
    try {
      await couponApi.update(coupon.id, { isActive: !coupon.isActive })
      setCoupons(prev => prev.map(c => c.id === coupon.id ? { ...c, isActive: !c.isActive } : c))
      toast.success(coupon.isActive ? 'Coupon deactivated' : 'Coupon activated')
    } catch {
      toast.error('Failed to toggle coupon')
    }
  }

  return (
    <AdminLayout>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="font-display text-2xl font-semibold">Coupons</h1>
            <p className="text-surface-400 text-sm mt-1">{total} coupon{total !== 1 ? 's' : ''} total</p>
          </div>
          <button onClick={() => setCreateOpen(true)} className="btn-primary flex items-center gap-2">
            <Plus className="w-4 h-4" /> New Coupon
          </button>
        </div>

        {/* Table */}
        <div className="card overflow-hidden">
          {loading ? (
            <div className="flex items-center justify-center py-20"><Spinner /></div>
          ) : coupons.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-20 text-surface-400 gap-3">
              <Tag className="w-8 h-8" />
              <p className="text-sm">No coupons yet. Create one to get started.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-surface-100 bg-surface-50 text-left text-xs text-surface-400 uppercase tracking-wide">
                    <th className="px-4 py-3">Code</th>
                    <th className="px-4 py-3">Type</th>
                    <th className="px-4 py-3">Value</th>
                    <th className="px-4 py-3">Min Order</th>
                    <th className="px-4 py-3">Usage</th>
                    <th className="px-4 py-3">Expires</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-surface-50">
                  {coupons.map(c => (
                    <tr key={c.id} className="hover:bg-surface-50 transition-colors">
                      <td className="px-4 py-3 font-mono font-semibold text-surface-900">{c.code}</td>
                      <td className="px-4 py-3 text-surface-500">
                        {c.type === 'Percentage' ? 'Percentage' : 'Fixed'}
                      </td>
                      <td className="px-4 py-3 font-medium">
                        {c.type === 'Percentage' ? `${c.value}%` : `₦${c.value.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}
                      </td>
                      <td className="px-4 py-3 text-surface-500">
                        {c.minimumOrderAmount ? `₦${c.minimumOrderAmount.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—'}
                      </td>
                      <td className="px-4 py-3 text-surface-500">
                        {c.usageLimit ? `${c.usedCount}/${c.usageLimit}` : `${c.usedCount} uses`}
                      </td>
                      <td className="px-4 py-3 text-surface-500">
                        {c.validTo ? format(new Date(c.validTo), 'MMM d, yyyy') : '—'}
                      </td>
                      <td className="px-4 py-3">
                        <button onClick={() => handleToggle(c)} title={c.isActive ? 'Deactivate' : 'Activate'}>
                          {c.isActive
                            ? <ToggleRight className="w-5 h-5 text-emerald-500" />
                            : <ToggleLeft  className="w-5 h-5 text-surface-300" />}
                        </button>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-1">
                          <button onClick={() => setEditTarget(c)} className="btn-icon text-surface-400 hover:text-brand-600">
                            <Edit2 className="w-3.5 h-3.5" />
                          </button>
                          <button onClick={() => setDeleteTarget(c)} className="btn-icon text-surface-400 hover:text-red-500">
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

        {total > pageSize && (
          <Pagination page={page} totalPages={Math.ceil(total / pageSize)} onChange={setPage} />
        )}
      </div>

      {/* Create Modal */}
      <Modal open={createOpen} onClose={() => setCreateOpen(false)} title="New Coupon">
        <CouponForm onSave={handleCreate} onClose={() => setCreateOpen(false)} saving={saving} />
      </Modal>

      {/* Edit Modal */}
      <Modal open={!!editTarget} onClose={() => setEditTarget(null)} title="Edit Coupon">
        {editTarget && (
          <CouponForm
            initial={{
              code:                  editTarget.code,
              description:           editTarget.description ?? '',
              type:                  editTarget.type,
              value:                 editTarget.value,
              minimumOrderAmount:    editTarget.minimumOrderAmount ?? '',
              maximumDiscountAmount: editTarget.maximumDiscountAmount ?? '',
              usageLimit:            editTarget.usageLimit ?? '',
              validFrom:             toFormDate(editTarget.validFrom),
              validTo:               toFormDate(editTarget.validTo),
              isActive:              editTarget.isActive,
            }}
            onSave={handleEdit}
            onClose={() => setEditTarget(null)}
            saving={saving}
          />
        )}
      </Modal>

      {/* Delete Modal */}
      <Modal open={!!deleteTarget} onClose={() => setDeleteTarget(null)} title="Delete Coupon">
        {deleteTarget && (
          <DeleteModal
            coupon={deleteTarget}
            onConfirm={handleDelete}
            onClose={() => setDeleteTarget(null)}
            deleting={deleting}
          />
        )}
      </Modal>
    </AdminLayout>
  )
}
