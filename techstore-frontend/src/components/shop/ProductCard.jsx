import { Link } from 'react-router-dom'
import { ShoppingCart, Star, Package, Heart } from 'lucide-react'
import { clsx } from 'clsx'
import toast from 'react-hot-toast'
import { useState } from 'react'
import { cartApi } from '../../services/api'
import { useAuthStore, useCartStore, useUIStore } from '../../store'
import { Spinner, Price } from '../ui'

export function ProductCard({ product }) {
  const { isAuthenticated } = useAuthStore()
  const { setCart, isInCart } = useCartStore()
  const { toggleCart } = useUIStore()
  const [loading, setLoading] = useState(false)
  const [wished, setWished] = useState(false)
  const inCart = isInCart(product.id)

  const addToCart = async (e) => {
    e.preventDefault()
    if (!isAuthenticated) { toast.error('Please sign in to add to cart'); return }
    if (inCart) { toggleCart(); return }
    setLoading(true)
    try {
      const { data } = await cartApi.addItem({ productId: product.id, quantity: 1 })
      setCart(data)
      toast.success('Added to cart!')
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not add to cart')
    } finally { setLoading(false) }
  }

  const outOfStock = (product.quantityAvailable ?? 1) <= 0

  return (
    <Link to={`/products/${product.id}`}
      className="card-hover group relative flex flex-col overflow-hidden">
      {/* Image */}
      <div className="relative aspect-square bg-surface-50 overflow-hidden">
        {product.imageUrl
          ? <img src={product.imageUrl} alt={product.name}
              className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
          : <div className="w-full h-full flex items-center justify-center">
              <Package className="w-16 h-16 text-surface-200" />
            </div>
        }
        {/* Badges */}
        <div className="absolute top-3 left-3 flex flex-col gap-1.5">
          {product.compareAtPrice && product.compareAtPrice > product.price && (
            <span className="badge bg-red-500 text-white text-[10px]">
              -{Math.round((1 - product.price / product.compareAtPrice) * 100)}%
            </span>
          )}
          {outOfStock && <span className="badge-gray text-[10px]">Out of stock</span>}
        </div>
        {/* Wishlist */}
        <button onClick={e => { e.preventDefault(); setWished(v => !v) }}
          className={clsx(
            'absolute top-3 right-3 w-8 h-8 rounded-full bg-white shadow flex items-center justify-center opacity-0 group-hover:opacity-100 transition-all duration-200',
            wished && 'opacity-100'
          )}>
          <Heart className={clsx('w-3.5 h-3.5', wished ? 'fill-red-500 text-red-500' : 'text-surface-400')} />
        </button>
      </div>

      {/* Info */}
      <div className="p-4 flex flex-col flex-1 gap-2">
        {product.categoryName && (
          <p className="text-[11px] font-medium text-brand-500 uppercase tracking-wider">{product.categoryName}</p>
        )}
        <h3 className="font-medium text-surface-900 text-sm leading-snug line-clamp-2 group-hover:text-brand-600 transition-colors">
          {product.name}
        </h3>

        {product.rating && (
          <div className="flex items-center gap-1">
            <Star className="w-3 h-3 fill-amber-400 text-amber-400" />
            <span className="text-xs text-surface-500">{product.rating.toFixed(1)} ({product.reviewCount ?? 0})</span>
          </div>
        )}

        <div className="mt-auto flex items-center justify-between pt-2">
          <Price value={product.price} compare={product.compareAtPrice} />
          <button onClick={addToCart} disabled={loading || outOfStock}
            className={clsx(
              'flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium transition-all duration-200',
              outOfStock
                ? 'bg-surface-100 text-surface-400 cursor-not-allowed'
                : inCart
                  ? 'bg-emerald-50 text-emerald-700 hover:bg-emerald-100'
                  : 'bg-brand-500 text-white hover:bg-brand-600 active:scale-95'
            )}>
            {loading ? <Spinner size="sm" /> : <ShoppingCart className="w-3 h-3" />}
            {inCart ? 'In Cart' : outOfStock ? 'Sold Out' : 'Add'}
          </button>
        </div>
      </div>
    </Link>
  )
}

export function ProductGrid({ products, loading }) {
  if (loading) {
    return (
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
        {Array(8).fill(0).map((_, i) => (
          <div key={i} className="card overflow-hidden animate-pulse">
            <div className="aspect-square bg-surface-100" />
            <div className="p-4 space-y-2">
              <div className="h-3 bg-surface-100 rounded w-1/2" />
              <div className="h-4 bg-surface-100 rounded w-3/4" />
              <div className="h-4 bg-surface-100 rounded w-1/3" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  if (!products?.length) return null

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 animate-stagger">
      {products.map(p => <ProductCard key={p.id} product={p} />)}
    </div>
  )
}
