import { useEffect, useRef, useState } from 'react'
import { Plus, Search, Edit2, Trash2, Package, Image as ImageIcon, X, RefreshCw } from 'lucide-react'
import { productApi, imageApi } from '../../services/api'
import { AdminLayout } from '../../components/layout/AdminLayout'
import { Modal, Input, Select, Spinner, Pagination } from '../../components/ui'
import { useForm, useWatch } from 'react-hook-form'
import toast from 'react-hot-toast'
import { Link } from 'react-router-dom'

// ── SKU generator ─────────────────────────────────────────────────────────────
const CATEGORY_CODES = { 1:'PHN', 2:'TAB', 3:'WCH', 4:'LAP', 5:'ACC' }

function generateSKU(categoryId) {
  const code   = CATEGORY_CODES[categoryId] ?? 'PRD'
  const random = Math.random().toString(36).substring(2, 6).toUpperCase()
  return `TS-${code}-${random}`
}

// ── Image Picker Modal ────────────────────────────────────────────────────────
function ImagePickerModal({ open, onClose, onSelect }) {
  const [images, setImages]   = useState([])
  const [loading, setLoading] = useState(false)
  const fileRef = useRef()

  const load = () => {
    setLoading(true)
    imageApi.getAll().then(r => setImages(r.data)).catch(() => toast.error('Failed to load images')).finally(() => setLoading(false))
  }

  useEffect(() => { if (open) load() }, [open])

  const handleUpload = async (files) => {
    if (!files?.length) return
    const formData = new FormData()
    Array.from(files).forEach(f => formData.append('files', f))
    try {
      const { data } = await imageApi.upload(formData)
      if (data.uploaded?.length) toast.success(`${data.uploaded.length} image(s) uploaded`)
      if (data.errors?.length) data.errors.forEach(e => toast.error(e))
      load()
    } catch { toast.error('Upload failed') }
  }

  return (
    <Modal open={open} onClose={onClose} title="Image Library" size="xl">
      <div className="space-y-4">
        <div className="flex items-center gap-3">
          <button onClick={() => fileRef.current?.click()} className="btn-secondary gap-2 text-sm">
            <Plus className="w-4 h-4" /> Upload New
          </button>
          <input ref={fileRef} type="file" multiple accept="image/*" className="hidden"
            onChange={e => handleUpload(e.target.files)} />
          <button onClick={load} className="btn-icon"><RefreshCw className="w-4 h-4" /></button>
          <p className="text-surface-400 text-xs ml-auto">Click an image to select it</p>
        </div>
        {loading ? (
          <div className="flex justify-center py-12"><Spinner size="lg" /></div>
        ) : images.length === 0 ? (
          <div className="text-center py-12">
            <ImageIcon className="w-10 h-10 text-surface-200 mx-auto mb-2" />
            <p className="text-surface-400 text-sm">No images yet. Upload some first.</p>
          </div>
        ) : (
          <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 gap-3 max-h-[400px] overflow-y-auto pr-1">
            {images.map(img => (
              <button key={img.fileName} onClick={() => { onSelect(img.url); onClose() }}
                className="group relative aspect-square rounded-xl overflow-hidden border-2 border-transparent hover:border-brand-400 transition-all duration-150 bg-surface-50">
                <img src={img.url} alt={img.fileName} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200" />
                <div className="absolute inset-0 bg-brand-500/0 group-hover:bg-brand-500/10 transition-all flex items-center justify-center">
                  <div className="opacity-0 group-hover:opacity-100 bg-white rounded-full px-2 py-1 text-[10px] font-medium text-brand-600 shadow">Select</div>
                </div>
              </button>
            ))}
          </div>
        )}
        <div className="flex justify-between items-center pt-2 border-t border-surface-100">
          <Link to="/admin/images" onClick={onClose} className="text-xs text-brand-500 hover:text-brand-600">Manage full library →</Link>
          <button onClick={onClose} className="btn-secondary btn-sm">Cancel</button>
        </div>
      </div>
    </Modal>
  )
}

// ── Product Form Modal ────────────────────────────────────────────────────────
function ProductFormModal({ open, onClose, product, categories, onSaved }) {
  const { register, handleSubmit, reset, setValue, control, formState: { errors } } = useForm()
  const [loading, setLoading]       = useState(false)
  const [pickerOpen, setPickerOpen] = useState(false)
  const isEdit = !!product

  const imageUrl   = useWatch({ control, name: 'imageUrl',   defaultValue: '' })
  const categoryId = useWatch({ control, name: 'categoryId', defaultValue: '' })

  useEffect(() => {
    if (product) reset({ ...product, isActive: product.isActive?.toString(), isFeatured: product.isFeatured?.toString() })
    else reset({ initialStock: 0, lowStockThreshold: 10 })
  }, [product, open])

  useEffect(() => {
    if (!isEdit && categoryId) setValue('sku', generateSKU(parseInt(categoryId)))
  }, [categoryId, isEdit])

  const refreshSKU = () => { if (!isEdit && categoryId) setValue('sku', generateSKU(parseInt(categoryId))) }

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
          isActive:   data.isActive   === 'true' || data.isActive   === true,
          isFeatured: data.isFeatured === 'true' || data.isFeatured === true,
        })
        toast.success('Product updated')
      } else {
        await productApi.create({
          name: data.name, description: data.description, sku: data.sku,
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
      onSaved(); onClose()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to save product')
    } finally { setLoading(false) }
  }

  return (
    <>
      <Modal open={open} onClose={onClose} title={isEdit ? 'Edit Product' : 'New Product'} size="lg">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Product Name" className="col-span-2" error={errors.name?.message}
              {...register('name', { required: 'Required' })} />

            {!isEdit && (
              <div className="col-span-2 sm:col-span-1">
                <label className="label flex items-center justify-between">
                  SKU
                  <span className="text-[10px] font-normal text-surface-400 normal-case tracking-normal">
                    Unique product code — auto-generated
                  </span>
                </label>
                <div className="flex gap-2">
                  <input className="input flex-1" placeholder="Select a category first"
                    {...register('sku', { required: 'Required' })} />
                  <button type="button" onClick={refreshSKU} title="Regenerate SKU"
                    className="btn-secondary px-3 flex-shrink-0">
                    <RefreshCw className="w-4 h-4" />
                  </button>
                </div>
                {errors.sku && <p className="text-red-500 text-xs mt-1">{errors.sku.message}</p>}
              </div>
            )}

            <Input label="Brand" {...register('brand')} />
            <Input label="Price ($)" type="number" step="0.01" error={errors.price?.message}
              {...register('price', { required: 'Required', min: { value: 0.01, message: 'Must be > 0' } })} />
            <Input label="Compare-at Price ($)" type="number" step="0.01" {...register('compareAtPrice')} />

            <Select label="Category" error={errors.categoryId?.message}
              {...register('categoryId', { required: 'Required' })}>
              <option value="">Select category…</option>
              {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </Select>

            {isEdit ? (
              <>
                <Select label="Status" {...register('isActive')}>
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
                <Input label="Low Stock Threshold" type="number" defaultValue="10" {...register('lowStockThreshold')} />
              </>
            )}

            {/* Image field + library picker */}
            <div className="col-span-2">
              <label className="label">Product Image</label>
              <div className="flex gap-2">
                <input className="input flex-1" placeholder="https://… or pick from library below"
                  {...register('imageUrl')} />
                <button type="button" onClick={() => setPickerOpen(true)}
                  className="btn-secondary px-4 flex-shrink-0 gap-2 text-sm">
                  <ImageIcon className="w-4 h-4" /> Library
                </button>
              </div>

              {/* Live preview */}
              {imageUrl && (
                <div className="mt-3 flex items-start gap-3 p-3 bg-surface-50 rounded-xl border border-surface-200">
                  <div className="w-20 h-20 rounded-xl overflow-hidden bg-surface-100 flex-shrink-0 border border-surface-200">
                    <img src={imageUrl} alt="Preview" className="w-full h-full object-contain p-2"
                      onError={e => { e.target.style.display='none'; e.target.nextSibling.style.display='flex' }} />
                    <div className="w-full h-full hidden items-center justify-center text-surface-300">
                      <ImageIcon className="w-6 h-6" />
                    </div>
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-xs font-medium text-surface-600 mb-1">Preview</p>
                    <p className="text-xs text-surface-400 truncate">{imageUrl}</p>
                    <button type="button" onClick={() => setValue('imageUrl', '')}
                      className="mt-2 text-xs text-red-500 hover:text-red-600 flex items-center gap-1">
                      <X className="w-3 h-3" /> Remove image
                    </button>
                  </div>
                </div>
              )}
            </div>
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

      <ImagePickerModal open={pickerOpen} onClose={() => setPickerOpen(false)}
        onSelect={url => setValue('imageUrl', url)} />
    </>
  )
}

// ── Main Products Page ────────────────────────────────────────────────────────
export default function AdminProducts() {
  const [products, setProducts]     = useState([])
  const [categories, setCategories] = useState([])
  const [total, setTotal]           = useState(0)
  const [loading, setLoading]       = useState(true)
  const [page, setPage]             = useState(1)
  const [query, setQuery]           = useState('')
  const [modalOpen, setModalOpen]   = useState(false)
  const [editing, setEditing]       = useState(null)

  const pageSize   = 20
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
    try { await productApi.delete(id); toast.success('Product deactivated'); load() }
    catch { toast.error('Failed to deactivate') }
  }

  return (
    <AdminLayout>
      <div className="space-y-6 max-w-[1400px]">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="font-display text-2xl font-semibold">Products</h1>
            <p className="text-surface-400 text-sm mt-0.5">{total} total products</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <Link to="/admin/images" className="btn-secondary gap-2 text-sm">
              <ImageIcon className="w-4 h-4" /> Image Library
            </Link>
            <button onClick={() => { setEditing(null); setModalOpen(true) }} className="btn-primary gap-2">
              <Plus className="w-4 h-4" /> Add Product
            </button>
          </div>
        </div>

        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400" />
          <input className="input pl-9" placeholder="Search products, SKU…"
            value={query} onChange={e => { setQuery(e.target.value); setPage(1) }} />
        </div>

        <div className="table-container">
          <table className="data-table">
            <thead><tr><th>Product</th><th>SKU</th><th>Category</th><th>Price</th><th>Stock</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7} className="text-center py-12"><Spinner /></td></tr>
              ) : products.length === 0 ? (
                <tr><td colSpan={7} className="text-center py-12 text-surface-400">No products found</td></tr>
              ) : products.map(p => (
                <tr key={p.id}>
                  <td>
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-xl bg-white flex-shrink-0 overflow-hidden border border-surface-200">
                        {p.imageUrl
                          ? <img src={p.imageUrl} alt={p.name} className="w-full h-full object-contain p-1" />
                          : <div className="w-full h-full flex items-center justify-center"><Package className="w-4 h-4 text-surface-300" /></div>}
                      </div>
                      <div className="min-w-0">
                        <p className="font-medium text-surface-900 truncate max-w-[200px]">{p.name}</p>
                        {p.brand && <p className="text-xs text-surface-400">{p.brand}</p>}
                      </div>
                    </div>
                  </td>
                  <td><span className="font-mono text-xs text-surface-500 bg-surface-50 px-2 py-1 rounded-lg">{p.sku}</span></td>
                  <td><span className="badge-gray text-xs">{p.categoryName}</span></td>
                  <td>
                    <p className="font-semibold text-surface-900">₦{p.price.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
                    {p.compareAtPrice && <p className="text-xs text-surface-400 line-through">₦{p.compareAtPrice.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>}
                  </td>
                  <td>
                    <span className={`font-medium text-sm ${(p.quantityAvailable??0)<=0?'text-red-500':(p.quantityAvailable??0)<=10?'text-amber-600':'text-emerald-600'}`}>
                      {p.quantityAvailable ?? 0}
                    </span>
                  </td>
                  <td><span className={`badge ${p.isActive ? 'badge-green' : 'badge-gray'}`}>{p.isActive ? 'Active' : 'Inactive'}</span></td>
                  <td>
                    <div className="flex items-center gap-1">
                      <button onClick={() => { setEditing(p); setModalOpen(true) }} className="btn-icon"><Edit2 className="w-3.5 h-3.5" /></button>
                      <button onClick={() => handleDelete(p.id, p.name)} className="btn-icon text-red-400 hover:text-red-600 hover:bg-red-50"><Trash2 className="w-3.5 h-3.5" /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <Pagination page={page} totalPages={totalPages} onChange={setPage} />
        <ProductFormModal open={modalOpen} onClose={() => setModalOpen(false)}
          product={editing} categories={categories} onSaved={load} />
      </div>
    </AdminLayout>
  )
}