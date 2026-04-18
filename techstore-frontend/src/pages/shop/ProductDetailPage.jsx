import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { ShoppingCart, ArrowLeft, Star, Shield, Truck, Package, CheckCircle } from 'lucide-react'
import { productApi, cartApi } from '../../services/api'
import { useAuthStore, useCartStore, useUIStore } from '../../store'
import { Navbar, CartDrawer, Footer } from '../../components/layout/ShopLayout'
import { LoadingPage, Price, StatusBadge } from '../../components/ui'
import toast from 'react-hot-toast'

export default function ProductDetailPage() {
  const { id } = useParams()
  const [product, setProduct] = useState(null)
  const [loading, setLoading] = useState(true)
  const [qty, setQty] = useState(1)
  const [adding, setAdding] = useState(false)
  const { isAuthenticated } = useAuthStore()
  const { setCart, isInCart } = useCartStore()
  const { toggleCart } = useUIStore()

  useEffect(() => {
    productApi.getById(id).then(r => setProduct(r.data)).finally(() => setLoading(false))
  }, [id])

  if (loading) return <><Navbar /><LoadingPage /></>
  if (!product) return <><Navbar /><div className="page-container py-16 text-center text-surface-400">Product not found</div></>

  const inCart = isInCart(product.id)
  const outOfStock = (product.quantityAvailable ?? 1) <= 0

  const addToCart = async () => {
    if (!isAuthenticated) { toast.error('Please sign in to add to cart'); return }
    if (inCart) { toggleCart(); return }
    setAdding(true)
    try {
      const { data } = await cartApi.addItem({ productId: product.id, quantity: qty })
      setCart(data)
      toast.success(`${qty}× ${product.name} added to cart!`)
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not add to cart')
    } finally { setAdding(false) }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <CartDrawer />
      <main className="flex-1 page-container py-8">
        <Link to="/products" className="inline-flex items-center gap-2 text-sm text-surface-500 hover:text-surface-900 mb-6 transition-colors">
          <ArrowLeft className="w-4 h-4" /> Back to Products
        </Link>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 animate-fade-up">
          {/* Image */}
          <div className="aspect-square bg-surface-50 rounded-3xl overflow-hidden relative">
            {product.imageUrl
              ? <img src={product.imageUrl} alt={product.name} className="w-full h-full object-cover" />
              : <div className="w-full h-full flex items-center justify-center">
                  <Package className="w-24 h-24 text-surface-200" />
                </div>
            }
            {product.compareAtPrice && product.compareAtPrice > product.price && (
              <div className="absolute top-4 left-4 badge bg-red-500 text-white">
                Save {Math.round((1 - product.price / product.compareAtPrice) * 100)}%
              </div>
            )}
          </div>

          {/* Info */}
          <div className="flex flex-col">
            {product.categoryName && (
              <Link to={`/products?categoryId=${product.categoryId}`}
                className="text-sm font-medium text-brand-500 hover:text-brand-600 mb-2">
                {product.categoryName}
              </Link>
            )}
            <h1 className="font-display text-3xl font-semibold text-surface-900 mb-1">{product.name}</h1>
            {product.brand && <p className="text-surface-500 text-sm mb-3">by {product.brand}</p>}

            {product.rating && (
              <div className="flex items-center gap-2 mb-4">
                <div className="flex items-center gap-0.5">
                  {[1,2,3,4,5].map(s => (
                    <Star key={s} className={`w-4 h-4 ${s <= Math.round(product.rating) ? 'fill-amber-400 text-amber-400' : 'text-surface-200'}`} />
                  ))}
                </div>
                <span className="text-sm text-surface-500">{product.rating.toFixed(1)} ({product.reviewCount} reviews)</span>
              </div>
            )}

            <div className="flex items-baseline gap-3 mb-6">
              <span className="font-display text-4xl font-bold text-surface-900">${product.price.toFixed(2)}</span>
              {product.compareAtPrice && product.compareAtPrice > product.price && (
                <span className="text-xl text-surface-400 line-through">${product.compareAtPrice.toFixed(2)}</span>
              )}
            </div>

            {product.description && (
              <p className="text-surface-600 leading-relaxed mb-6">{product.description}</p>
            )}

            {/* SKU + Stock */}
            <div className="flex items-center gap-4 text-sm text-surface-500 mb-6">
              <span>SKU: <span className="font-mono">{product.sku}</span></span>
              <span className={`flex items-center gap-1 ${outOfStock ? 'text-red-500' : 'text-emerald-600'}`}>
                <CheckCircle className="w-3.5 h-3.5" />
                {outOfStock ? 'Out of Stock' : `${product.quantityAvailable} in stock`}
              </span>
            </div>

            {/* Qty + Add to cart */}
            {!outOfStock && (
              <div className="flex items-center gap-3 mb-6">
                <div className="flex items-center border border-surface-200 rounded-xl overflow-hidden">
                  <button onClick={() => setQty(q => Math.max(1, q - 1))}
                    className="px-3 py-2.5 text-surface-600 hover:bg-surface-50 transition-colors text-lg font-medium">−</button>
                  <span className="px-4 py-2.5 text-sm font-semibold border-x border-surface-200 min-w-[3rem] text-center">{qty}</span>
                  <button onClick={() => setQty(q => Math.min(product.quantityAvailable ?? 10, q + 1))}
                    className="px-3 py-2.5 text-surface-600 hover:bg-surface-50 transition-colors text-lg font-medium">+</button>
                </div>
                <button onClick={addToCart} disabled={adding}
                  className={`btn btn-lg flex-1 ${inCart ? 'btn-secondary border-emerald-300 text-emerald-700' : 'btn-primary'}`}>
                  <ShoppingCart className="w-4 h-4" />
                  {adding ? 'Adding…' : inCart ? 'View Cart' : 'Add to Cart'}
                </button>
              </div>
            )}

            {/* Trust badges */}
            <div className="grid grid-cols-2 gap-3 pt-6 border-t border-surface-100">
              {[
                { icon: Truck,  text: 'Free shipping over $100' },
                { icon: Shield, text: '2-year warranty included' },
              ].map(b => (
                <div key={b.text} className="flex items-center gap-2 text-sm text-surface-500">
                  <b.icon className="w-4 h-4 text-brand-500" /> {b.text}
                </div>
              ))}
            </div>
          </div>
        </div>
      </main>
      <Footer />
    </div>
  )
}
