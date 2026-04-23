import { useEffect, useRef, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import {
  ShoppingCart, ArrowLeft, Star, Shield, Truck, Package,
  CheckCircle, Heart, Share2, ChevronRight, Minus, Plus,
  AlertTriangle, ThumbsUp, MessageSquare, ZoomIn
} from 'lucide-react'
import { productApi, cartApi, reviewApi } from '../../services/api'
import { useAuthStore, useCartStore, useUIStore } from '../../store'
import { Navbar, CartDrawer, Footer } from '../../components/layout/ShopLayout'
import { LoadingPage, Spinner } from '../../components/ui'
import { ProductGrid } from '../../components/shop/ProductCard'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

// ── Star display ──────────────────────────────────────────────────────────────
function Stars({ rating, size = 'sm', interactive = false, onRate }) {
  const [hover, setHover] = useState(0)
  const sz = size === 'sm' ? 'w-4 h-4' : size === 'md' ? 'w-5 h-5' : 'w-6 h-6'
  return (
    <div className="flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map(s => (
        <Star key={s}
          className={`${sz} transition-colors ${interactive ? 'cursor-pointer' : ''}
            ${s <= (interactive ? (hover || rating) : Math.round(rating))
              ? 'fill-amber-400 text-amber-400'
              : 'text-surface-200 fill-surface-200'}`}
          onMouseEnter={() => interactive && setHover(s)}
          onMouseLeave={() => interactive && setHover(0)}
          onClick={() => interactive && onRate?.(s)}
        />
      ))}
    </div>
  )
}

// ── Rating bar row ────────────────────────────────────────────────────────────
function RatingBar({ label, count, total }) {
  const pct = total > 0 ? Math.round((count / total) * 100) : 0
  return (
    <div className="flex items-center gap-2 text-sm">
      <span className="text-surface-500 w-12 flex-shrink-0 text-right">{label}</span>
      <div className="flex-1 bg-surface-100 rounded-full h-2.5 overflow-hidden">
        <div className="bg-amber-400 h-full rounded-full transition-all duration-500"
          style={{ width: `${pct}%` }} />
      </div>
      <span className="text-surface-400 w-8 flex-shrink-0 text-xs">{pct}%</span>
    </div>
  )
}

// ── Review card ───────────────────────────────────────────────────────────────
function ReviewCard({ review }) {
  return (
    <div className="py-5 border-b border-surface-100 last:border-0">
      <div className="flex items-start justify-between gap-4 mb-2">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-full bg-brand-100 flex items-center justify-center text-brand-700 text-xs font-semibold">
            {review.userName?.[0]?.toUpperCase()}
          </div>
          <div>
            <p className="text-sm font-medium text-surface-900">{review.userName}</p>
            {review.isVerifiedPurchase && (
              <span className="text-[11px] text-emerald-600 flex items-center gap-0.5">
                <CheckCircle className="w-3 h-3" /> Verified Purchase
              </span>
            )}
          </div>
        </div>
        <span className="text-xs text-surface-400 flex-shrink-0">
          {format(new Date(review.createdAt), 'MMM d, yyyy')}
        </span>
      </div>
      <Stars rating={review.rating} size="sm" />
      {review.title && (
        <p className="font-semibold text-surface-900 mt-2 text-sm">{review.title}</p>
      )}
      {review.comment && (
        <p className="text-surface-600 text-sm mt-1 leading-relaxed">{review.comment}</p>
      )}
    </div>
  )
}

// ── Write review form ─────────────────────────────────────────────────────────
function ReviewForm({ productId, onSubmitted }) {
  const [rating, setRating]   = useState(0)
  const [title, setTitle]     = useState('')
  const [comment, setComment] = useState('')
  const [loading, setLoading] = useState(false)

  const submit = async (e) => {
    e.preventDefault()
    if (!rating) { toast.error('Please select a star rating'); return }
    setLoading(true)
    try {
      await reviewApi.create({ productId, rating, title: title || null, comment: comment || null })
      toast.success('Review submitted!')
      setRating(0); setTitle(''); setComment('')
      onSubmitted()
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not submit review')
    } finally { setLoading(false) }
  }

  return (
    <form onSubmit={submit} className="bg-surface-50 rounded-2xl p-5 space-y-4">
      <h3 className="font-semibold text-surface-900">Write a Review</h3>
      <div>
        <p className="text-sm text-surface-600 mb-2">Your rating</p>
        <Stars rating={rating} size="lg" interactive onRate={setRating} />
      </div>
      <div>
        <label className="label">Title (optional)</label>
        <input className="input" placeholder="Sum up your experience…"
          value={title} onChange={e => setTitle(e.target.value)} maxLength={100} />
      </div>
      <div>
        <label className="label">Review</label>
        <textarea className="input min-h-[100px] resize-y" placeholder="What did you like or dislike?"
          value={comment} onChange={e => setComment(e.target.value)} maxLength={1000} />
      </div>
      <button type="submit" disabled={loading} className="btn-primary">
        {loading ? <Spinner size="sm" /> : 'Submit Review'}
      </button>
    </form>
  )
}

// ── Image gallery ─────────────────────────────────────────────────────────────
function ImageGallery({ imageUrl, name }) {
  const [zoomed, setZoomed] = useState(false)
  const [pos, setPos]       = useState({ x: 50, y: 50 })
  const imgRef = useRef()

  const handleMouseMove = (e) => {
    const { left, top, width, height } = imgRef.current.getBoundingClientRect()
    const x = ((e.clientX - left) / width) * 100
    const y = ((e.clientY - top) / height) * 100
    setPos({ x, y })
  }

  if (!imageUrl) return (
    <div className="aspect-square bg-surface-50 rounded-2xl flex items-center justify-center">
      <Package className="w-24 h-24 text-surface-200" />
    </div>
  )

  return (
    <div className="space-y-3">
      {/* Main image */}
      <div
        ref={imgRef}
        className="relative aspect-square bg-surface-50 rounded-2xl overflow-hidden cursor-zoom-in group"
        onMouseEnter={() => setZoomed(true)}
        onMouseLeave={() => setZoomed(false)}
        onMouseMove={handleMouseMove}
      >
        <img
          src={imageUrl} alt={name}
          className="w-full h-full object-contain transition-transform duration-100"
          style={zoomed ? {
            transform: 'scale(2)',
            transformOrigin: `${pos.x}% ${pos.y}%`
          } : {}}
        />
        {!zoomed && (
          <div className="absolute bottom-3 right-3 bg-white/80 backdrop-blur-sm rounded-lg px-2 py-1 flex items-center gap-1.5 text-xs text-surface-600 opacity-0 group-hover:opacity-100 transition-opacity">
            <ZoomIn className="w-3.5 h-3.5" /> Hover to zoom
          </div>
        )}
      </div>
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────────
export default function ProductDetailPage() {
  const { id }       = useParams()
  const navigate     = useNavigate()
  const reviewsRef   = useRef()

  const [product, setProduct]       = useState(null)
  const [loading, setLoading]       = useState(true)
  const [qty, setQty]               = useState(1)
  const [adding, setAdding]         = useState(false)
  const [wished, setWished]         = useState(false)
  const [activeTab, setActiveTab]   = useState('description')
  const [reviews, setReviews]       = useState([])
  const [summary, setSummary]       = useState(null)
  const [reviewsLoading, setReviewsLoading] = useState(false)
  const [related, setRelated]       = useState([])

  const { isAuthenticated } = useAuthStore()
  const { setCart, isInCart } = useCartStore()
  const { toggleCart } = useUIStore()

  useEffect(() => {
    setLoading(true)
    productApi.getById(id)
      .then(r => {
        setProduct(r.data)
        // load related products from same category
        if (r.data?.categoryId) {
          productApi.byCategory(r.data.categoryId, { page: 1, pageSize: 5 })
            .then(rel => setRelated(rel.data.filter(p => p.id !== parseInt(id)).slice(0, 4)))
            .catch(() => {})
        }
      })
      .finally(() => setLoading(false))
  }, [id])

  const loadReviews = () => {
    setReviewsLoading(true)
    Promise.all([
      reviewApi.getByProduct(id, { page: 1, pageSize: 10 }),
      reviewApi.getSummary(id),
    ]).then(([rev, sum]) => {
      setReviews(rev.data?.items ?? rev.data ?? [])
      setSummary(sum.data)
    }).catch(() => {})
    .finally(() => setReviewsLoading(false))
  }

  useEffect(() => {
    if (activeTab === 'reviews') loadReviews()
  }, [activeTab, id])

  if (loading) return <><Navbar /><LoadingPage /></>
  if (!product) return (
    <><Navbar />
    <div className="page-container py-16 text-center">
      <p className="text-surface-400">Product not found</p>
      <Link to="/products" className="btn-primary mt-4 inline-flex">Browse Products</Link>
    </div></>
  )

  const inCart    = isInCart(product.id)
  const outOfStock = (product.quantityAvailable ?? 1) <= 0
  const lowStock   = !outOfStock && (product.quantityAvailable ?? 10) <= 5
  const savings    = product.compareAtPrice && product.compareAtPrice > product.price
    ? product.compareAtPrice - product.price : null
  const savingsPct = savings ? Math.round((savings / product.compareAtPrice) * 100) : null

  const addToCart = async () => {
    if (!isAuthenticated) { toast.error('Please sign in to add to cart'); return }
    if (inCart) { toggleCart(); return }
    setAdding(true)
    try {
      const { data } = await cartApi.addItem({ productId: product.id, quantity: qty })
      setCart(data)
      toast.success(`${qty}× ${product.name} added!`)
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not add to cart')
    } finally { setAdding(false) }
  }

  const buyNow = async () => {
    if (!isAuthenticated) { navigate('/login'); return }
    setAdding(true)
    try {
      const { data } = await cartApi.addItem({ productId: product.id, quantity: qty })
      setCart(data)
      navigate('/checkout')
    } catch (err) {
      toast.error(err.response?.data?.message || 'Could not proceed')
    } finally { setAdding(false) }
  }

  const scrollToReviews = () => {
    setActiveTab('reviews')
    setTimeout(() => reviewsRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 100)
  }

  const share = () => {
    navigator.clipboard.writeText(window.location.href)
    toast.success('Link copied!')
  }

  return (
    <div className="min-h-screen flex flex-col bg-white">
      <Navbar />
      <CartDrawer />

      <main className="flex-1">
        {/* Breadcrumb */}
        <div className="bg-surface-50 border-b border-surface-100">
          <div className="page-container py-2.5 flex items-center gap-1.5 text-xs text-surface-400">
            <Link to="/" className="hover:text-brand-500">Home</Link>
            <ChevronRight className="w-3 h-3" />
            <Link to="/products" className="hover:text-brand-500">Products</Link>
            {product.categoryName && <>
              <ChevronRight className="w-3 h-3" />
              <Link to={`/products?categoryId=${product.categoryId}`} className="hover:text-brand-500">
                {product.categoryName}
              </Link>
            </>}
            <ChevronRight className="w-3 h-3" />
            <span className="text-surface-600 truncate max-w-[200px]">{product.name}</span>
          </div>
        </div>

        {/* ── Main product section ── */}
        <div className="page-container py-8">
          <div className="grid grid-cols-1 lg:grid-cols-[1fr_420px] xl:grid-cols-[1fr_460px] gap-10">

            {/* LEFT — Image gallery */}
            <div className="lg:sticky lg:top-24 lg:self-start">
              <ImageGallery imageUrl={product.imageUrl} name={product.name} />
            </div>

            {/* RIGHT — Product info + buy box */}
            <div className="space-y-5">
              {/* Brand + category */}
              <div className="flex items-center gap-2 flex-wrap">
                {product.categoryName && (
                  <Link to={`/products?categoryId=${product.categoryId}`}
                    className="badge-blue text-xs hover:bg-brand-100 transition-colors">
                    {product.categoryName}
                  </Link>
                )}
                {product.brand && (
                  <span className="text-sm text-surface-500">
                    by <span className="text-brand-600 font-medium hover:underline cursor-pointer">{product.brand}</span>
                  </span>
                )}
              </div>

              {/* Title */}
              <h1 className="font-display text-2xl sm:text-3xl font-semibold text-surface-900 leading-tight">
                {product.name}
              </h1>

              {/* Rating row */}
              {product.rating ? (
                <button onClick={scrollToReviews}
                  className="flex items-center gap-2 hover:opacity-80 transition-opacity">
                  <Stars rating={product.rating} size="sm" />
                  <span className="text-brand-600 text-sm underline underline-offset-2">
                    {product.rating.toFixed(1)}
                  </span>
                  <span className="text-surface-400 text-sm">
                    ({product.reviewCount} {product.reviewCount === 1 ? 'review' : 'reviews'})
                  </span>
                </button>
              ) : (
                <button onClick={scrollToReviews} className="text-sm text-brand-600 hover:underline">
                  Be the first to review
                </button>
              )}

              <hr className="border-surface-100" />

              {/* Price block */}
              <div className="space-y-1">
                {savings && (
                  <div className="flex items-center gap-2">
                    <span className="badge bg-red-500 text-white text-xs">-{savingsPct}% OFF</span>
                    <span className="text-sm text-red-600 font-medium">
                      You save ₦{savings.toFixed(2)}
                    </span>
                  </div>
                )}
                <div className="flex items-baseline gap-3">
                  <span className="text-4xl font-display font-bold text-surface-900">
                    ₦{product.price.toFixed(2)}
                  </span>
                  {product.compareAtPrice && product.compareAtPrice > product.price && (
                    <span className="text-lg text-surface-400 line-through">
                      ₦{product.compareAtPrice.toFixed(2)}
                    </span>
                  )}
                </div>
                <p className="text-xs text-surface-400">Prices include VAT</p>
              </div>

              <hr className="border-surface-100" />

              {/* Stock status */}
              <div>
                {outOfStock ? (
                  <div className="flex items-center gap-2 text-red-500">
                    <AlertTriangle className="w-4 h-4" />
                    <span className="font-medium">Out of Stock</span>
                  </div>
                ) : lowStock ? (
                  <div className="flex items-center gap-2 text-amber-600">
                    <AlertTriangle className="w-4 h-4" />
                    <span className="font-medium">Only {product.quantityAvailable} left in stock — order soon</span>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 text-emerald-600">
                    <CheckCircle className="w-4 h-4" />
                    <span className="font-medium">In Stock</span>
                    <span className="text-surface-400 text-sm">({product.quantityAvailable} available)</span>
                  </div>
                )}
              </div>

              {/* Delivery estimate */}
              <div className="bg-surface-50 rounded-xl p-4 space-y-2.5">
                <div className="flex items-start gap-2.5 text-sm">
                  <Truck className="w-4 h-4 text-brand-500 mt-0.5 flex-shrink-0" />
                  <div>
                    <span className="font-medium text-surface-900">Free Delivery</span>
                    <span className="text-surface-500"> on orders over ₦100</span>
                    <p className="text-surface-400 text-xs mt-0.5">Usually ships in 1–2 business days</p>
                  </div>
                </div>
                <div className="flex items-start gap-2.5 text-sm">
                  <Shield className="w-4 h-4 text-brand-500 mt-0.5 flex-shrink-0" />
                  <div>
                    <span className="font-medium text-surface-900">2-Year Warranty</span>
                    <p className="text-surface-400 text-xs mt-0.5">Manufacturer warranty included</p>
                  </div>
                </div>
              </div>

              {/* Quantity + Buttons */}
              {!outOfStock && (
                <div className="space-y-3">
                  {/* Qty */}
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-medium text-surface-700 w-16">Quantity:</span>
                    <div className="flex items-center border border-surface-200 rounded-xl overflow-hidden">
                      <button onClick={() => setQty(q => Math.max(1, q - 1))}
                        className="px-3 py-2 text-surface-600 hover:bg-surface-50 transition-colors">
                        <Minus className="w-4 h-4" />
                      </button>
                      <span className="px-5 py-2 text-sm font-semibold border-x border-surface-200 min-w-[3rem] text-center">
                        {qty}
                      </span>
                      <button onClick={() => setQty(q => Math.min(product.quantityAvailable ?? 10, q + 1))}
                        className="px-3 py-2 text-surface-600 hover:bg-surface-50 transition-colors">
                        <Plus className="w-4 h-4" />
                      </button>
                    </div>
                  </div>

                  {/* Add to cart */}
                  <button onClick={addToCart} disabled={adding}
                    className={`w-full btn btn-lg gap-2 ${inCart
                      ? 'bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-100'
                      : 'btn-primary'}`}>
                    {adding ? <Spinner size="sm" /> : <ShoppingCart className="w-5 h-5" />}
                    {adding ? 'Adding…' : inCart ? '✓ Added — View Cart' : 'Add to Cart'}
                  </button>

                  {/* Buy now */}
                  {!inCart && (
                    <button onClick={buyNow} disabled={adding}
                      className="w-full btn btn-lg bg-amber-400 hover:bg-amber-500 text-surface-900 font-semibold gap-2">
                      Buy Now
                    </button>
                  )}
                </div>
              )}

              {/* Wishlist + Share */}
              <div className="flex items-center gap-3 pt-1">
                <button onClick={() => setWished(v => !v)}
                  className={`flex items-center gap-2 text-sm transition-colors ${wished ? 'text-red-500' : 'text-surface-500 hover:text-red-400'}`}>
                  <Heart className={`w-4 h-4 ${wished ? 'fill-red-500' : ''}`} />
                  {wished ? 'Wishlisted' : 'Add to Wishlist'}
                </button>
                <span className="text-surface-200">|</span>
                <button onClick={share} className="flex items-center gap-2 text-sm text-surface-500 hover:text-brand-600 transition-colors">
                  <Share2 className="w-4 h-4" /> Share
                </button>
              </div>

              {/* SKU */}
              <p className="text-xs text-surface-400">
                SKU: <span className="font-mono">{product.sku}</span>
              </p>
            </div>
          </div>
        </div>

        {/* ── Tabs section ── */}
        <div ref={reviewsRef} className="border-t border-surface-100">
          <div className="page-container">
            {/* Tab headers */}
            <div className="flex border-b border-surface-100 overflow-x-auto scrollbar-hide">
              {[
                { key: 'description',    label: 'Description' },
                { key: 'specifications', label: 'Specifications' },
                { key: 'reviews',        label: `Reviews ${product.reviewCount ? `(${product.reviewCount})` : ''}` },
              ].map(tab => (
                <button key={tab.key} onClick={() => setActiveTab(tab.key)}
                  className={`px-6 py-4 text-sm font-medium whitespace-nowrap border-b-2 transition-colors
                    ${activeTab === tab.key
                      ? 'border-brand-500 text-brand-600'
                      : 'border-transparent text-surface-500 hover:text-surface-900'}`}>
                  {tab.label}
                </button>
              ))}
            </div>

            {/* Tab content */}
            <div className="py-8 max-w-3xl">

              {/* Description */}
              {activeTab === 'description' && (
                <div className="prose prose-sm max-w-none">
                  {product.description ? (
                    <p className="text-surface-700 leading-relaxed text-base">{product.description}</p>
                  ) : (
                    <p className="text-surface-400 italic">No description available.</p>
                  )}
                </div>
              )}

              {/* Specifications */}
              {activeTab === 'specifications' && (
                <div className="space-y-0 rounded-xl overflow-hidden border border-surface-200">
                  {[
                    { label: 'Brand',    value: product.brand },
                    { label: 'SKU',      value: product.sku },
                    { label: 'Category', value: product.categoryName },
                    { label: 'Rating',   value: product.rating ? `${product.rating.toFixed(1)} / 5` : 'No ratings yet' },
                    { label: 'Stock',    value: outOfStock ? 'Out of stock' : `${product.quantityAvailable} units available` },
                    { label: 'Price',    value: `₦${product.price.toFixed(2)}` },
                  ].filter(r => r.value).map((row, i) => (
                    <div key={row.label}
                      className={`flex text-sm ${i % 2 === 0 ? 'bg-surface-50' : 'bg-white'}`}>
                      <span className="w-36 flex-shrink-0 px-4 py-3 font-medium text-surface-700 border-r border-surface-200">
                        {row.label}
                      </span>
                      <span className="px-4 py-3 text-surface-600">{row.value}</span>
                    </div>
                  ))}
                </div>
              )}

              {/* Reviews */}
              {activeTab === 'reviews' && (
                <div className="space-y-8">
                  {/* Summary */}
                  {summary && summary.totalReviews > 0 && (
                    <div className="flex flex-col sm:flex-row gap-8 p-6 bg-surface-50 rounded-2xl">
                      {/* Big number */}
                      <div className="text-center flex-shrink-0">
                        <p className="text-6xl font-display font-bold text-surface-900">
                          {(summary.averageRating ?? 0).toFixed(1)}
                        </p>
                        <Stars rating={summary.averageRating ?? 0} size="md" />
                        <p className="text-surface-400 text-sm mt-1">{summary.totalReviews} reviews</p>
                      </div>
                      {/* Bars */}
                      <div className="flex-1 space-y-2">
                        <RatingBar label="5 ★" count={summary.rating5Count} total={summary.totalReviews} />
                        <RatingBar label="4 ★" count={summary.rating4Count} total={summary.totalReviews} />
                        <RatingBar label="3 ★" count={summary.rating3Count} total={summary.totalReviews} />
                        <RatingBar label="2 ★" count={summary.rating2Count} total={summary.totalReviews} />
                        <RatingBar label="1 ★" count={summary.rating1Count} total={summary.totalReviews} />
                      </div>
                    </div>
                  )}

                  {/* Write review */}
                  {isAuthenticated ? (
                    <ReviewForm productId={parseInt(id)} onSubmitted={() => { loadReviews(); productApi.getById(id).then(r => setProduct(r.data)) }} />
                  ) : (
                    <div className="bg-surface-50 rounded-2xl p-5 text-center">
                      <MessageSquare className="w-8 h-8 text-surface-300 mx-auto mb-2" />
                      <p className="text-surface-600 text-sm mb-3">Sign in to write a review</p>
                      <Link to="/login" className="btn-primary btn-sm">Sign In</Link>
                    </div>
                  )}

                  {/* Review list */}
                  {reviewsLoading ? (
                    <div className="flex justify-center py-8"><Spinner /></div>
                  ) : reviews.length === 0 ? (
                    <p className="text-surface-400 text-sm text-center py-8">No reviews yet. Be the first!</p>
                  ) : (
                    <div>
                      <h3 className="font-semibold text-surface-900 mb-2">
                        {reviews.length} Customer {reviews.length === 1 ? 'Review' : 'Reviews'}
                      </h3>
                      {reviews.map(r => <ReviewCard key={r.id} review={r} />)}
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* ── Related products ── */}
        {related.length > 0 && (
          <div className="border-t border-surface-100">
            <div className="page-container py-10">
              <div className="flex items-center justify-between mb-6">
                <h2 className="font-display text-xl font-semibold">Related Products</h2>
                <Link to={`/products?categoryId=${product.categoryId}`}
                  className="text-sm text-brand-600 hover:underline">
                  View all in {product.categoryName} →
                </Link>
              </div>
              <ProductGrid products={related} loading={false} />
            </div>
          </div>
        )}
      </main>

      <Footer />
    </div>
  )
}