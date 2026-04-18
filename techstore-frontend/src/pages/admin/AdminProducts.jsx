import { useEffect, useState } from 'react'
import { Plus, Search, Edit2, Trash2, Package } from 'lucide-react'
import { productApi, inventoryApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, Input, Select, StatusBadge, Spinner, EmptyState, Pagination } from '../../components/ui'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'

function ProductFormModal({ open, onClose, product, categories, onSaved }) {
  const { register, handleSubmit, reset, formState: { errors } } = useForm()
  const [loading, setLoading] = useState(false)
  const isEdit = !!product

  useEffect(() => {
    if (product) reset(product)
    else reset({})
  }, [product])

  const onSubmit = async (data) => {
    setLoading(true)
    try {
      if (isEdit) {
        await productApi.update(product.id, {
          name: data.name, description: data.description,
          price: parseFloat(data.price),
          compareAtPrice: data.compareAtPrice ? parseFloat(data.compareAtPrice) : null,
          brand: data.brand, imageUrl: data.imageUrl,
          categoryId: parseInt(data.categoryId),
          isActive: data.isActive === 'true' || data.isActive === true,
          isFeatured: data.isFeatured === 'true' || data.isFeatured === true,
        })
        toast.success('Product updated')
      } else {
        await productApi.create({
          name: data.name, description: data.description,
          sku: data.sku,
          price: parseFloat(data.price),
          compareAtPrice: data.compareAtPrice ? parseFloat(data.compareAtPrice) : null,
          brand: data.brand, imageUrl: data.imageUrl,
          categoryId: parseInt(data.categoryId),
          initialStock: parseInt(data.initialStock || 0),
          lowStockThreshold: parseInt(data.lowStockThreshold || 10),
          isActive: true, isFeatured: false,
        })
        toast.success('Product created')
      }
      onSaved()
      onClose()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to save product')
    } finally { setLoading(false) }
  }

  return (
    <Modal open={open} onClose={onClose} title={isEdit ? 'Edit Product' : 'New Product'} size="lg">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input label="Product Name" className="col-span-2" error={errors.name?.message}
            {...register('name', { required: 'Required' })} />
          {!isEdit && (
            <Input label="SKU" error={errors.sku?.message}
              {...register('sku', { required: 'Required' })} />
          )}
          <Input label="Brand" {...register('brand')} />
          <Input label="Price ($)" type="number" step="0.01" error={errors.price?.message}
            {...register('price', { required: 'Required', min: { value: 0.01, message: 'Must be > 0' } })} />
          <Input label="Compare-at Price ($)" type="number" step="0.01"
            {...register('compareAtPrice')} />
          <Select label="Category" error={errors.categoryId?.message}
            {...register('categoryId', { required: 'Required' })}>
            <option value="">Select…</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </Select>
          {isEdit ? (
            <>
              <Select label="Active" {...register('isActive')}>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </Select>
              <Select label="Featured" {...register('isFeatured')}>
                <option value="false">No</option>
                <option value="true">Yes</option>
              </Select>
            </>
          ) : (
            <>
              <Input label="Initial Stock" type="number" defaultValue="0" {...register('initialStock')} />
              <Input label="Low Stock Alert Threshold" type="number" defaultValue="10" {...register('lowStockThreshold')} />
            </>
          )}
          <Input label="Image URL" className="col-span-2" {...register('imageUrl')} />
        </div>
        <div>
          <label className="label">Description</label>
          <textarea className="input min-h-[80px] resize-y" {...register('description')} />
        </div>
        <div className="flex justify-end gap-3 pt-2">
          <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={loading} className="btn-primary">
            {loading ? <Spinner size="sm" /> : isEdit ? 'Save Changes' : 'Create Product'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

export default function AdminProducts() {
  const [products, setProducts] = useState([])
  const [categories, setCategories] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [query, setQuery] = useState('')
  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState(null)

  const pageSize = 20
  const totalPages = Math.ceil(total / pageSize)

  const load = () => {
    setLoading(true)
    productApi.search({ query, page, pageSize, sortBy: 'newest', sortDir: 'desc' })
      .then(r => { setProducts(r.data.items); setTotal(r.data.total) })
      .finally(() => setLoading(false))
  }

  useEffect(() => { productApi.categories().then(r => setCategories(r.data)) }, [])
  useEffect(() => { load() }, [page, query])

  const handleDelete = async (id, name) => {
    if (!confirm(`Deactivate "${name}"?`)) return
    try {
      await productApi.delete(id)
      toast.success('Product deactivated')
      load()
    } catch { toast.error('Failed to deactivate') }
  }

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="font-display text-2xl font-semibold">Products</h1>
            <p className="text-surface-400 text-sm mt-0.5">{total} total products</p>
          </div>
          <button onClick={() => { setEditing(null); setModalOpen(true) }} className="btn-primary">
            <Plus className="w-4 h-4" /> Add Product
          </button>
        </div>

        {/* Search */}
        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400" />
          <input className="input pl-9" placeholder="Search products, SKU…"
            value={query} onChange={e => { setQuery(e.target.value); setPage(1) }} />
        </div>

        {/* Table */}
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>SKU</th>
                <th>Category</th>
                <th>Price</th>
                <th>Stock</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="text-center py-12"><Spinner /></td></tr>
              ) : products.length === 0 ? (
                <tr><td colSpan={7} className="text-center py-12 text-surface-400">No products found</td></tr>
              ) : products.map(p => (
                <tr key={p.id}>
                  <td>
                    <div className="flex items-center gap-3">
                      <div className="w-9 h-9 rounded-lg bg-surface-100 flex-shrink-0 overflow-hidden">
                        {p.imageUrl
                          ? <img src={p.imageUrl} alt={p.name} className="w-full h-full object-cover" />
                          : <div className="w-full h-full flex items-center justify-center"><Package className="w-4 h-4 text-surface-300" /></div>}
                      </div>
                      <div className="min-w-0">
                        <p className="font-medium text-surface-900 truncate max-w-[200px]">{p.name}</p>
                        {p.brand && <p className="text-xs text-surface-400">{p.brand}</p>}
                      </div>
                    </div>
                  </td>
                  <td><span className="font-mono text-xs text-surface-500">{p.sku}</span></td>
                  <td><span className="badge-gray text-xs">{p.categoryName}</span></td>
                  <td>
                    <div>
                      <p className="font-semibold text-surface-900">${p.price.toFixed(2)}</p>
                      {p.compareAtPrice && <p className="text-xs text-surface-400 line-through">${p.compareAtPrice.toFixed(2)}</p>}
                    </div>
                  </td>
                  <td>
                    <span className={`font-medium text-sm ${(p.quantityAvailable ?? 0) <= 0 ? 'text-red-500' : (p.quantityAvailable ?? 0) <= 10 ? 'text-amber-600' : 'text-emerald-600'}`}>
                      {p.quantityAvailable ?? 0}
                    </span>
                  </td>
                  <td>
                    <span className={`badge ${p.isActive ? 'badge-green' : 'badge-gray'}`}>
                      {p.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>
                    <div className="flex items-center gap-1">
                      <button onClick={() => { setEditing(p); setModalOpen(true) }} className="btn-icon">
                        <Edit2 className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => handleDelete(p.id, p.name)} className="btn-icon text-red-400 hover:text-red-600 hover:bg-red-50">
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <Pagination page={page} totalPages={totalPages} onChange={setPage} />

        <ProductFormModal
          open={modalOpen}
          onClose={() => setModalOpen(false)}
          product={editing}
          categories={categories}
          onSaved={load}
        />
      </div>
    </AdminLayout>
  )
}
