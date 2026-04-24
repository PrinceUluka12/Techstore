import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { Heart, ShoppingCart, ArrowLeft } from 'lucide-react'
import toast from 'react-hot-toast'
import { useWishlistStore, useCartStore, useAuthStore } from '../../store'
import { cartApi } from '../../services/api'
import { Navbar, Footer } from '../../components/layout/ShopLayout'
import { Spinner } from '../../components/ui'

export default function WishlistPage() {
  const { items, fetch, remove, loading } = useWishlistStore()
  const { setCart } = useCartStore()

  useEffect(() => { fetch() }, [])

  const moveToCart = async (item) => {
    try {
      const { data: cart } = await cartApi.addItem({ productId: item.productId, quantity: 1 })
      setCart(cart)
      await remove(item.productId)
      toast.success(`${item.productName} moved to cart`)
    } catch {
      toast.error('Failed to add to cart')
    }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-1 page-container py-10">
        <Link to="/account/profile" className="inline-flex items-center gap-1.5 text-sm text-surface-500 hover:text-surface-900 mb-6">
          <ArrowLeft className="w-4 h-4" /> My Account
        </Link>

        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="font-display text-2xl font-semibold">Wishlist</h1>
            <p className="text-surface-400 text-sm mt-0.5">{items.length} saved item{items.length !== 1 ? 's' : ''}</p>
          </div>
        </div>

        {loading ? (
          <div className="flex justify-center py-20"><Spinner size="xl" /></div>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 gap-4 text-surface-400">
            <Heart className="w-10 h-10" />
            <p className="text-sm">Your wishlist is empty</p>
            <Link to="/products" className="btn-primary">Browse Products</Link>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {items.map(item => (
              <div key={item.id} className="card overflow-hidden group">
                <Link to={`/products/${item.productId}`} className="block aspect-square bg-surface-50 overflow-hidden">
                  {item.productImageUrl
                    ? <img src={item.productImageUrl} alt={item.productName}
                        className="w-full h-full object-contain p-4 group-hover:scale-105 transition-transform duration-300" />
                    : <div className="w-full h-full flex items-center justify-center text-surface-200">
                        <ShoppingCart className="w-8 h-8" />
                      </div>}
                </Link>
                <div className="p-4 space-y-3">
                  <div>
                    <Link to={`/products/${item.productId}`}
                      className="text-sm font-medium text-surface-900 hover:text-brand-600 line-clamp-2 leading-snug">
                      {item.productName}
                    </Link>
                    <p className="text-base font-semibold text-surface-900 mt-1">
                      ₦{item.price.toLocaleString('en-NG', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <button onClick={() => moveToCart(item)}
                      className="btn-primary flex-1 text-sm flex items-center justify-center gap-1.5">
                      <ShoppingCart className="w-3.5 h-3.5" /> Add to Cart
                    </button>
                    <button onClick={() => remove(item.productId)}
                      className="btn-icon text-surface-400 hover:text-red-500 border border-surface-200 rounded-lg px-2">
                      <Heart className="w-4 h-4 fill-current text-red-400" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>
      <Footer />
    </div>
  )
}
