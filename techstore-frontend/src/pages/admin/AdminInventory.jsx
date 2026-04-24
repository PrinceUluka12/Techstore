import { useEffect, useState } from 'react'
import { AlertTriangle, Plus, Minus, RefreshCw, Package } from 'lucide-react'
import { inventoryApi, productApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, Input, Spinner, EmptyState } from '../../components/ui'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'

function AdjustModal({ open, onClose, inventory, mode, onSaved }) {
  const { register, handleSubmit, reset, formState: { errors } } = useForm()
  const [loading, setLoading] = useState(false)

  useEffect(() => { if (open) reset({}) }, [open])

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      if (mode === 'restock') {
        await inventoryApi.restock(inventory.productId, {
          quantity: parseInt(data.quantity),
          supplier: data.supplier,
          notes: data.notes,
        })
        toast.success(`+${data.quantity} units restocked`)
      } else {
        const qty = mode === 'remove'
          ? -Math.abs(parseInt(data.quantity))
          : parseInt(data.quantity)
        await inventoryApi.adjust(inventory.productId, {
          quantity: qty,
          reason: data.reason,
          notes: data.notes,
        })
        toast.success(`Stock adjusted by ${qty > 0 ? '+' : ''}${qty}`)
      }
      onSaved()
      onClose()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Adjustment failed')
    } finally { setLoading(false) }
  }

  const titles = { restock: 'Restock Product', add: 'Add Stock', remove: 'Remove Stock', threshold: 'Update Threshold' }

  return (
    <Modal open={open} onClose={onClose} title={titles[mode] ?? 'Adjust Stock'} size="sm">
      {inventory && (
        <div className="mb-4 p-3 bg-surface-50 rounded-xl text-sm">
          <p className="font-medium">{inventory.productName}</p>
          <p className="text-surface-500 text-xs mt-0.5 font-mono">{inventory.productSKU}</p>
          <p className="text-surface-600 text-xs mt-1">
            Current stock: <strong>{inventory.quantityOnHand}</strong> on hand · <strong>{inventory.quantityAvailable}</strong> available
          </p>
        </div>
      )}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        {mode === 'threshold' ? (
          <Input label="New Low-Stock Threshold" type="number" error={errors.lowStockThreshold?.message}
            defaultValue={inventory?.lowStockThreshold}
            {...register('lowStockThreshold', { required: 'Required', min: { value: 1, message: 'Min 1' } })} />
        ) : (
          <>
            <Input label="Quantity" type="number" error={errors.quantity?.message}
              {...register('quantity', { required: 'Required', min: { value: 1, message: 'Must be ≥ 1' } })} />
            {mode === 'restock'
              ? <Input label="Supplier (optional)" {...register('supplier')} />
              : <Input label="Reason" error={errors.reason?.message}
                  {...register('reason', { required: 'Required' })} />}
            <Input label="Notes (optional)" {...register('notes')} />
          </>
        )}
        <div className="flex justify-end gap-3 pt-1">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={loading} className={mode === 'remove' ? 'btn-danger' : 'btn-primary'}>
            {loading ? <Spinner size="sm" /> : 'Confirm'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

export default function AdminInventory() {
  const [inventory, setInventory] = useState([])
  const [loading, setLoading] = useState(true)
  const [showLowOnly, setShowLowOnly] = useState(false)
  const [selected, setSelected] = useState(null)
  const [mode, setMode] = useState('restock')
  const [modalOpen, setModalOpen] = useState(false)

  const load = () => {
    setLoading(true)
    const fn = showLowOnly ? inventoryApi.lowStock() : productApi.search({ page: 1, pageSize: 100 })
      .then(r => Promise.all(r.data.items.map(p => inventoryApi.getByProduct(p.id).then(inv => inv.data).catch(() => null))))
      .then(invs => ({ data: invs.filter(Boolean) }))

    ;(showLowOnly ? inventoryApi.lowStock() : inventoryApi.lowStock().then(() => {
      return productApi.search({ page: 1, pageSize: 100 }).then(r =>
        Promise.all(r.data.items.map(p =>
          inventoryApi.getByProduct(p.id).then(inv => inv.data).catch(() => null)
        ))
      ).then(invs => ({ data: invs.filter(Boolean) }))
    }))

    if (showLowOnly) {
      inventoryApi.lowStock().then(r => setInventory(r.data)).finally(() => setLoading(false))
    } else {
      productApi.search({ page: 1, pageSize: 100 }).then(r =>
        Promise.all(r.data.items.map(p =>
          inventoryApi.getByProduct(p.id).then(inv => inv.data).catch(() => null)
        ))
      ).then(invs => setInventory(invs.filter(Boolean))).finally(() => setLoading(false))
    }
  }

  useEffect(() => { load() }, [showLowOnly])

  const openModal = (inv, m) => { setSelected(inv); setMode(m); setModalOpen(true) }

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Inventory</h1>
            <p className="text-surface-400 text-sm mt-0.5">{inventory.length} products tracked</p>
          </div>
          <button onClick={() => setShowLowOnly(v => !v)}
            className={`btn-secondary gap-2 ${showLowOnly ? 'border-amber-400 text-amber-700 bg-amber-50' : ''}`}>
            <AlertTriangle className="w-4 h-4" />
            {showLowOnly ? 'Show All' : 'Low Stock Only'}
          </button>
        </div>

        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>SKU</th>
                <th>On Hand</th>
                <th>Reserved</th>
                <th>Available</th>
                <th>Threshold</th>
                <th>Status</th>
                <th>Last Restocked</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={9} className="text-center py-12"><Spinner /></td></tr>
              ) : inventory.length === 0 ? (
                <tr><td colSpan={9} className="text-center py-12 text-surface-400">No inventory records</td></tr>
              ) : inventory.map(inv => {
                const isLow = inv.quantityAvailable <= inv.lowStockThreshold
                const isOut = inv.quantityAvailable <= 0
                return (
                  <tr key={inv.id}>
                    <td>
                      <div className="flex items-center gap-2">
                        <Package className="w-4 h-4 text-surface-300 flex-shrink-0" />
                        <span className="font-medium text-surface-900 truncate max-w-[180px]">{inv.productName}</span>
                      </div>
                    </td>
                    <td><span className="font-mono text-xs text-surface-500">{inv.productSKU}</span></td>
                    <td><span className="font-semibold">{inv.quantityOnHand}</span></td>
                    <td><span className="text-surface-500">{inv.quantityReserved}</span></td>
                    <td>
                      <span className={`font-semibold ${isOut ? 'text-red-500' : isLow ? 'text-amber-600' : 'text-emerald-600'}`}>
                        {inv.quantityAvailable}
                      </span>
                    </td>
                    <td><span className="text-surface-400">{inv.lowStockThreshold}</span></td>
                    <td>
                      <span className={`badge ${isOut ? 'badge-red' : isLow ? 'badge-yellow' : 'badge-green'}`}>
                        {isOut ? 'Out of Stock' : isLow ? 'Low Stock' : 'In Stock'}
                      </span>
                    </td>
                    <td>
                      <span className="text-xs text-surface-400">
                        {new Date(inv.lastRestockedAt).toLocaleDateString()}
                      </span>
                    </td>
                    <td>
                      <div className="flex items-center gap-1">
                        <button onClick={() => openModal(inv, 'restock')} className="btn-icon text-emerald-600 hover:bg-emerald-50" title="Restock">
                          <RefreshCw className="w-3.5 h-3.5" />
                        </button>
                        <button onClick={() => openModal(inv, 'add')} className="btn-icon text-brand-600 hover:bg-brand-50" title="Add stock">
                          <Plus className="w-3.5 h-3.5" />
                        </button>
                        <button onClick={() => openModal(inv, 'remove')} className="btn-icon text-red-400 hover:bg-red-50" title="Remove stock">
                          <Minus className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>

        <AdjustModal
          open={modalOpen}
          onClose={() => setModalOpen(false)}
          inventory={selected}
          mode={mode}
          onSaved={load}
        />
      </div>
    </AdminLayout>
  )
}
