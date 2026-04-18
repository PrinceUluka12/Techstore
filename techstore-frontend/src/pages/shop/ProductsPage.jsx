import { useState, useEffect, useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'
import { SlidersHorizontal, X, Search } from 'lucide-react'
import { productApi } from '../../services/api'
import { ProductGrid } from '../../components/shop/ProductCard'
import { Navbar, CartDrawer, Footer } from '../../components/layout/ShopLayout'
import { Pagination } from '../../components/ui'

export default function ProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [products, setProducts] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [categories, setCategories] = useState([])
  const [filtersOpen, setFiltersOpen] = useState(false)

  const page = parseInt(searchParams.get('page') || '1')
  const pageSize = 20
  const totalPages = Math.ceil(total / pageSize)

  const filters = {
    query:      searchParams.get('query') || '',
    categoryId: searchParams.get('categoryId') || '',
    minPrice:   searchParams.get('minPrice') || '',
    maxPrice:   searchParams.get('maxPrice') || '',
    sortBy:     searchParams.get('sortBy') || 'name',
    sortDir:    searchParams.get('sortDir') || 'asc',
    inStock:    searchParams.get('inStock') || '',
  }

  const setFilter = (key, val) => {
    const p = new URLSearchParams(searchParams)
    if (val) p.set(key, val); else p.delete(key)
    p.set('page', '1')
    setSearchParams(p)
  }

  useEffect(() => {
    productApi.categories().then(r => setCategories(r.data))
  }, [])

  useEffect(() => {
    setLoading(true)
    productApi.search({ ...filters, page, pageSize })
      .then(r => { setProducts(r.data.items); setTotal(r.data.total) })
      .finally(() => setLoading(false))
  }, [searchParams.toString()])

  const activeFilterCount = [filters.categoryId, filters.minPrice, filters.maxPrice, filters.inStock]
    .filter(Boolean).length

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <CartDrawer />
      <main className="flex-1 page-container py-8">

        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center gap-4 mb-6">
          <div className="flex-1">
            <h1 className="font-display text-2xl font-semibold text-surface-900">
              {filters.categoryId
                ? categories.find(c => c.id === parseInt(filters.categoryId))?.name ?? 'Products'
                : 'All Products'}
            </h1>
            {!loading && <p className="text-sm text-surface-400 mt-0.5">{total} results</p>}
          </div>

          {/* Search */}
          <div className="relative flex-1 max-w-xs">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-400" />
            <input className="input pl-9 text-sm" placeholder="Search products…"
              value={filters.query} onChange={e => setFilter('query', e.target.value)} />
          </div>

          {/* Sort */}
          <select className="input w-auto text-sm" value={`${filters.sortBy}|${filters.sortDir}`}
            onChange={e => {
              const [by, dir] = e.target.value.split('|')
              const p = new URLSearchParams(searchParams)
              p.set('sortBy', by); p.set('sortDir', dir)
              setSearchParams(p)
            }}>
            <option value="name|asc">Name A–Z</option>
            <option value="name|desc">Name Z–A</option>
            <option value="price|asc">Price Low–High</option>
            <option value="price|desc">Price High–Low</option>
            <option value="rating|desc">Top Rated</option>
            <option value="newest|desc">Newest</option>
          </select>

          <button onClick={() => setFiltersOpen(v => !v)}
            className={`btn-secondary gap-2 flex-shrink-0 ${activeFilterCount > 0 ? 'border-brand-400 text-brand-600' : ''}`}>
            <SlidersHorizontal className="w-4 h-4" />
            Filters
            {activeFilterCount > 0 && (
              <span className="w-4 h-4 rounded-full bg-brand-500 text-white text-[10px] font-bold flex items-center justify-center">
                {activeFilterCount}
              </span>
            )}
          </button>
        </div>

        {/* Filters panel */}
        {filtersOpen && (
          <div className="card p-5 mb-6 grid grid-cols-2 sm:grid-cols-4 gap-4 animate-fade-up">
            <div>
              <label className="label">Category</label>
              <select className="input text-sm" value={filters.categoryId} onChange={e => setFilter('categoryId', e.target.value)}>
                <option value="">All</option>
                {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Min Price</label>
              <input type="number" className="input text-sm" placeholder="$0"
                value={filters.minPrice} onChange={e => setFilter('minPrice', e.target.value)} />
            </div>
            <div>
              <label className="label">Max Price</label>
              <input type="number" className="input text-sm" placeholder="Any"
                value={filters.maxPrice} onChange={e => setFilter('maxPrice', e.target.value)} />
            </div>
            <div>
              <label className="label">Availability</label>
              <select className="input text-sm" value={filters.inStock} onChange={e => setFilter('inStock', e.target.value)}>
                <option value="">All</option>
                <option value="true">In Stock Only</option>
              </select>
            </div>
            {activeFilterCount > 0 && (
              <div className="col-span-full">
                <button onClick={() => setSearchParams({ page: '1' })}
                  className="flex items-center gap-1.5 text-sm text-red-500 hover:text-red-600">
                  <X className="w-3.5 h-3.5" /> Clear all filters
                </button>
              </div>
            )}
          </div>
        )}

        {/* Category quick-filter pills */}
        {categories.length > 0 && (
          <div className="flex gap-2 overflow-x-auto scrollbar-hide mb-6">
            <button onClick={() => setFilter('categoryId', '')}
              className={`badge flex-shrink-0 px-4 py-1.5 cursor-pointer ${!filters.categoryId ? 'badge-blue' : 'badge-gray hover:badge-blue'}`}>
              All
            </button>
            {categories.map(c => (
              <button key={c.id} onClick={() => setFilter('categoryId', c.id.toString())}
                className={`badge flex-shrink-0 px-4 py-1.5 cursor-pointer ${filters.categoryId === c.id.toString() ? 'badge-blue' : 'badge-gray hover:badge-blue'}`}>
                {c.name}
              </button>
            ))}
          </div>
        )}

        <ProductGrid products={products} loading={loading} />
        <Pagination page={page} totalPages={totalPages} onChange={p => setFilter('page', p.toString())} />
      </main>
      <Footer />
    </div>
  )
}
