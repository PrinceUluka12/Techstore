import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, Star, ChevronRight } from 'lucide-react'
import { productApi, cartApi } from '../../services/api'
import { ProductGrid } from '../../components/shop/ProductCard'
import { Navbar, CartDrawer, Footer } from '../../components/layout/ShopLayout'
import { useCartStore, useAuthStore } from '../../store'

// ── Perks — matching the reference image style ────────────────────────────────
const perks = [
  {
    title: 'Free Delivery',
    desc: "Let's surprise you this season",
    svg: (
      <svg viewBox="0 0 40 40" className="w-10 h-10" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M8 30c0 2.2 1.8 4 4 4s4-1.8 4-4-1.8-4-4-4-4 1.8-4 4zm20 0c0 2.2 1.8 4 4 4s4-1.8 4-4-1.8-4-4-4-4 1.8-4 4zM2 6h4l4 18h20l4-14H10" stroke="#8B0000" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
        <circle cx="12" cy="30" r="2" fill="#8B0000"/>
        <circle cx="32" cy="30" r="2" fill="#8B0000"/>
      </svg>
    ),
  },
  {
    title: 'Coupon Codes',
    desc: 'Free voucher and gift cards',
    svg: (
      <svg viewBox="0 0 40 40" className="w-10 h-10" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="8" y="12" width="24" height="16" rx="2" stroke="#8B0000" strokeWidth="2"/>
        <path d="M8 20h24M14 12v4M26 12v4M14 24v4M26 24v4" stroke="#8B0000" strokeWidth="2" strokeLinecap="round"/>
        <path d="M16 18l2 2 4-4" stroke="#8B0000" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
      </svg>
    ),
  },
  {
    title: 'Secured payments',
    desc: 'We accept all major credit cards.',
    svg: (
      <svg viewBox="0 0 40 40" className="w-10 h-10" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="4" y="10" width="32" height="20" rx="3" stroke="#8B0000" strokeWidth="2"/>
        <path d="M4 16h32" stroke="#8B0000" strokeWidth="2"/>
        <rect x="8" y="22" width="8" height="3" rx="1" fill="#8B0000"/>
        <rect x="20" y="22" width="5" height="3" rx="1" fill="#8B0000"/>
      </svg>
    ),
  },
  {
    title: 'Customer service',
    desc: 'Top notch customer support.',
    svg: (
      <svg viewBox="0 0 40 40" className="w-10 h-10" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M20 4C13.4 4 8 9.4 8 16c0 4 1.9 7.6 4.9 9.9L12 32l6.2-2.1c.6.1 1.2.1 1.8.1 6.6 0 12-5.4 12-12S26.6 4 20 4z" stroke="#8B0000" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
        <path d="M15 15h.01M20 15h.01M25 15h.01" stroke="#8B0000" strokeWidth="2.5" strokeLinecap="round"/>
      </svg>
    ),
  },
]

// ── Brand logos ───────────────────────────────────────────────────────────────
const brands = [
  { name: 'Samsung',  style: 'font-black text-xl tracking-tight text-surface-800' },
  { name: 'Nokia',    style: 'font-black text-xl tracking-wider uppercase text-surface-800' },
  { name: 'Apple',    style: 'font-semibold text-xl text-surface-800' },
  { name: 'Huawei',   style: 'font-bold text-lg tracking-wide text-surface-700' },
  { name: 'Infinix',  style: 'font-bold text-xl italic text-surface-800' },
  { name: 'Tecno',    style: 'font-black text-xl tracking-widest uppercase text-surface-800' },
]

// ── Category tiles — real device photos via Unsplash ─────────────────────────
const categories = [
  {
    id: 1,
    name: 'Smartphones',
    desc: 'Latest flagships',
    img: 'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=600&q=80&auto=format&fit=crop',
  },
  {
    id: 2,
    name: 'Tablets',
    desc: 'Work & play',
    img: 'https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=600&q=80&auto=format&fit=crop',
  },
  {
    id: 3,
    name: 'Smart Watches',
    desc: 'Stay connected',
    img: 'https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&q=80&auto=format&fit=crop',
  },
  {
    id: 4,
    name: 'Laptops',
    desc: 'Power & portability',
    img: 'https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=600&q=80&auto=format&fit=crop',
  },
]

// ── Stats ─────────────────────────────────────────────────────────────────────
const stats = [
  { val: '10k+', label: 'Customers' },
  { val: '500+', label: 'Products' },
  { val: '4.9★', label: 'Avg rating' },
  { val: '24h',  label: 'Support' },
]

export default function HomePage() {
  const [featured, setFeatured]   = useState([])
  const [hotItems, setHotItems]   = useState([])
  const [loading, setLoading]     = useState(true)
  const { setCart }               = useCartStore()
  const { isAuthenticated }       = useAuthStore()

  useEffect(() => {
    // Load featured products
    productApi.featured().then(r => setFeatured(r.data)).finally(() => setLoading(false))

    // Load "Hot right now" — newest products with images
    productApi.search({ page: 1, pageSize: 4, sortBy: 'newest', sortDir: 'desc' })
      .then(r => setHotItems((r.data.items ?? []).filter(p => p.imageUrl).slice(0, 3)))
      .catch(() => {})

    if (isAuthenticated) {
      cartApi.get().then(r => setCart(r.data)).catch(() => {})
    }
  }, [isAuthenticated])

  return (
    <div className="min-h-screen flex flex-col bg-white">
      <Navbar />
      <CartDrawer />
      <main className="flex-1">

        {/* ── HERO ── */}
        <section className="relative bg-[#0b1154] overflow-hidden">
          <div className="absolute inset-0 opacity-[0.035]"
            style={{
              backgroundImage: 'linear-gradient(rgba(255,255,255,.8) 1px,transparent 1px),linear-gradient(90deg,rgba(255,255,255,.8) 1px,transparent 1px)',
              backgroundSize: '32px 32px'
            }} />
          <div className="absolute -top-20 right-1/4 w-[400px] h-[400px] rounded-full bg-brand-600 blur-[120px] opacity-25 pointer-events-none" />
          <div className="absolute bottom-0 right-10 w-[220px] h-[220px] rounded-full bg-violet-700 blur-[100px] opacity-20 pointer-events-none" />

          <div className="page-container relative">
            <div className="grid lg:grid-cols-[1fr_320px] xl:grid-cols-[1fr_360px] gap-0 min-h-[500px]">

              {/* Left */}
              <div className="flex flex-col justify-center py-16 pr-0 lg:pr-12 border-r border-white/10">
                <div className="inline-flex items-center gap-2 self-start bg-white/8 border border-white/15 rounded-full px-3 py-1.5 mb-6">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
                  <span className="text-white/70 text-xs font-medium">New arrivals just dropped</span>
                </div>

                <h1 className="font-display leading-[0.95] tracking-tight mb-5">
                  <span className="block text-5xl sm:text-6xl xl:text-7xl font-black text-white">
                    Premium tech,
                  </span>
                  <span
                    className="block text-5xl sm:text-6xl xl:text-7xl font-black text-transparent"
                    style={{ WebkitTextStroke: '2px #2d52ff' }}>
                    delivered fast.
                  </span>
                </h1>

                <p className="text-white/50 text-base leading-relaxed max-w-md mb-8">
                  Nigeria's favourite online electronics store. Phones, tablets, watches and more — from brands you trust.
                </p>

                <div className="flex flex-wrap gap-3">
                  <Link to="/products"
                    className="inline-flex items-center gap-2 bg-brand-500 hover:bg-brand-600 text-white font-semibold px-6 py-3 rounded-xl text-sm transition-all duration-200 active:scale-95">
                    Shop Now <ArrowRight className="w-4 h-4" />
                  </Link>
                  <Link to="/products?sortBy=price&sortDir=asc"
                    className="inline-flex items-center gap-2 bg-white/8 hover:bg-white/15 border border-white/15 text-white/90 font-medium px-6 py-3 rounded-xl text-sm transition-all duration-200">
                    View Deals
                  </Link>
                </div>

                {/* Category image pills */}
                <div className="flex flex-wrap gap-2 mt-8">
                  {categories.map(cat => (
                    <Link key={cat.id} to={`/products?categoryId=${cat.id}`}
                      className="flex items-center gap-2 bg-white/6 hover:bg-white/12 border border-white/10 hover:border-white/20 rounded-xl px-3 py-2 transition-all duration-200 group">
                      {/* Real category image thumbnail */}
                      <div className="w-5 h-5 rounded-md overflow-hidden flex-shrink-0">
                        <img src={cat.img} alt={cat.name}
                          className="w-full h-full object-cover opacity-80 group-hover:opacity-100 transition-opacity"
                          onError={e => e.target.style.display = 'none'} />
                      </div>
                      <span className="text-white/60 group-hover:text-white/90 text-xs font-medium transition-colors">
                        {cat.name}
                      </span>
                    </Link>
                  ))}
                </div>
              </div>

              {/* Right — Hot right now with real product images */}
              <div className="hidden lg:flex flex-col justify-center pl-10 py-16">
                <p className="text-[10px] font-bold text-white/30 uppercase tracking-[0.15em] mb-4">
                  Hot right now
                </p>

                <div className="space-y-3">
                  {hotItems.length > 0 ? hotItems.map(item => (
                    <Link key={item.id} to={`/products/${item.id}`}
                      className="flex items-center gap-3 bg-white/5 hover:bg-white/10 border border-white/8 hover:border-white/15 rounded-xl p-3 transition-all duration-200 group">
                      <div className="w-12 h-12 rounded-xl overflow-hidden flex-shrink-0 bg-white/8">
                        <img src={item.imageUrl} alt={item.name}
                          className="w-full h-full object-cover" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-white/80 text-sm font-medium truncate group-hover:text-white transition-colors">
                          {item.name}
                        </p>
                        <p className="text-brand-400 text-xs font-semibold">₦{item.price.toFixed(2)}</p>
                      </div>
                      <ChevronRight className="w-4 h-4 text-white/20 group-hover:text-white/50 flex-shrink-0 transition-colors" />
                    </Link>
                  )) : (
                    // Fallback skeleton while loading
                    [1,2,3].map(i => (
                      <div key={i} className="flex items-center gap-3 bg-white/5 border border-white/8 rounded-xl p-3 animate-pulse">
                        <div className="w-12 h-12 rounded-xl bg-white/10 flex-shrink-0" />
                        <div className="flex-1 space-y-2">
                          <div className="h-3 bg-white/10 rounded w-3/4" />
                          <div className="h-2.5 bg-white/8 rounded w-1/2" />
                        </div>
                      </div>
                    ))
                  )}
                  <Link to="/products"
                    className="flex items-center justify-center gap-1.5 text-white/40 hover:text-white/70 text-xs py-2 transition-colors">
                    View all deals <ArrowRight className="w-3 h-3" />
                  </Link>
                </div>

                {/* Stats */}
                <div className="mt-8 grid grid-cols-2 gap-3">
                  {stats.map(s => (
                    <div key={s.label} className="bg-white/5 border border-white/8 rounded-xl p-3 text-center">
                      <p className="text-white font-bold text-lg leading-none">{s.val}</p>
                      <p className="text-white/40 text-[10px] mt-1">{s.label}</p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* ── PERKS STRIP ── */}
        <section className="border-b border-surface-100 bg-white">
          <div className="page-container py-6">
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-x-4 gap-y-6">
              {perks.map((p, i) => (
                <div key={p.title}
                  className={`flex items-center gap-3 sm:gap-4 ${i > 0 ? 'lg:border-l lg:border-surface-100 lg:pl-6' : ''}`}>
                  {/* Custom SVG icon */}
                  <div className="flex-shrink-0">
                    {p.svg}
                  </div>
                  <div>
                    <p className="text-sm font-bold text-surface-900">{p.title}</p>
                    <p className="text-xs text-surface-500 mt-0.5">{p.desc}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* ── BRAND LOGOS ROW ── */}
        <section className="border-b border-surface-100 bg-white">
          <div className="page-container py-7">
            <div className="flex items-center justify-between gap-6 overflow-x-auto scrollbar-hide">
              {brands.map(b => (
                <Link key={b.name} to={`/products?query=${b.name}`}
                  className="flex-shrink-0 flex items-center gap-2 group opacity-60 hover:opacity-100 transition-opacity duration-200">
                  {/* Apple gets the  logo */}
                  {b.name === 'Apple' && (
                    <svg viewBox="0 0 24 24" className="w-5 h-5 fill-surface-800" xmlns="http://www.w3.org/2000/svg">
                      <path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.8-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11"/>
                    </svg>
                  )}
                  <span className={b.style}>{b.name}</span>
                </Link>
              ))}
            </div>
          </div>
        </section>

        {/* ── CATEGORIES — real photo tiles ── */}
        <section className="page-container py-16">
          <div className="flex items-end justify-between mb-8">
            <div>
              <p className="text-[10px] font-bold text-surface-400 uppercase tracking-[0.15em] mb-1">Browse</p>
              <h2 className="font-display text-3xl font-black text-surface-900 leading-none tracking-tight">
                Shop by<br />
                <span className="text-transparent" style={{ WebkitTextStroke: '1.5px #2d52ff' }}>
                  category.
                </span>
              </h2>
            </div>
            <Link to="/products"
              className="text-sm text-brand-600 hover:text-brand-700 flex items-center gap-1 font-medium">
              All products <ArrowRight className="w-4 h-4" />
            </Link>
          </div>

          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {categories.map((cat, i) => (
              <Link key={cat.id} to={`/products?categoryId=${cat.id}`}
                className={`group relative rounded-2xl overflow-hidden
                  hover:ring-2 hover:ring-brand-500 hover:ring-offset-2 transition-all duration-300
                  ${i === 0 ? 'lg:row-span-2' : ''}`}
                style={{ minHeight: i === 0 ? '340px' : '160px' }}>

                {/* Real photo background */}
                <img src={cat.img} alt={cat.name}
                  className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-105" />

                {/* Gradient overlay */}
                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-black/10" />

                {/* Text */}
                <div className="absolute bottom-4 left-4 right-4">
                  <p className="font-display font-bold text-white text-lg leading-none drop-shadow-sm">
                    {cat.name}
                  </p>
                  <p className="text-white/60 text-xs mt-1">{cat.desc}</p>
                  <div className="flex items-center gap-1 mt-2.5 text-brand-300 text-xs font-semibold opacity-0 group-hover:opacity-100 transition-all duration-200 translate-y-1 group-hover:translate-y-0">
                    Shop now <ArrowRight className="w-3 h-3" />
                  </div>
                </div>
              </Link>
            ))}
          </div>
        </section>

        {/* ── FEATURED PRODUCTS ── */}
        <section className="border-t-2 border-surface-950 bg-white">
          <div className="page-container py-12">
            <div className="flex items-end justify-between mb-8">
              <div>
                <p className="text-[10px] font-bold text-surface-400 uppercase tracking-[0.15em] mb-1">Picked for you</p>
                <h2 className="font-display text-3xl font-black text-surface-900 leading-none tracking-tight">
                  Featured<br />
                  <span className="text-transparent" style={{ WebkitTextStroke: '1.5px #09090b' }}>
                    products.
                  </span>
                </h2>
              </div>
              <Link to="/products"
                className="text-sm text-brand-600 hover:text-brand-700 flex items-center gap-1 font-medium">
                View all <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
            <ProductGrid products={featured} loading={loading} />
          </div>
        </section>

        {/* ── DEALS + SOCIAL PROOF ── */}
        <section className="page-container py-10">
          <div className="grid lg:grid-cols-2 gap-4">

            {/* Deals CTA with real photo */}
            <div className="relative rounded-2xl overflow-hidden min-h-[220px]">
              <img
                src="https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=800&q=80&auto=format&fit=crop"
                alt="Tech deals"
                className="absolute inset-0 w-full h-full object-cover"
              />
              <div className="absolute inset-0 bg-gradient-to-r from-black/80 via-black/50 to-transparent" />
              <div className="relative p-8 flex flex-col justify-between h-full">
                <div>
                  <span className="inline-block text-[10px] font-bold uppercase tracking-[0.12em] text-brand-400 mb-3">
                    Limited Time
                  </span>
                  <h3 className="font-display text-3xl font-black text-white leading-none tracking-tight">
                    Up to 40% off<br />
                    <span className="text-transparent" style={{ WebkitTextStroke: '1.5px #fff' }}>
                      last season.
                    </span>
                  </h3>
                </div>
                <Link to="/products?sortBy=price&sortDir=asc"
                  className="self-start mt-6 inline-flex items-center gap-2 bg-white text-surface-900 font-semibold px-5 py-2.5 rounded-xl text-sm hover:bg-surface-100 transition-colors">
                  Shop Deals <ArrowRight className="w-4 h-4" />
                </Link>
              </div>
            </div>

            {/* Social proof */}
            <div className="rounded-2xl border border-surface-200 bg-surface-50 p-8 flex flex-col justify-between">
              <div>
                <div className="flex items-center gap-1 mb-1">
                  {[1,2,3,4,5].map(s => (
                    <Star key={s} className="w-5 h-5 fill-amber-400 text-amber-400" />
                  ))}
                  <span className="ml-2 text-sm font-bold text-surface-900">4.9 / 5</span>
                </div>
                <p className="text-surface-500 text-xs mb-5">Based on 2,400+ verified reviews</p>
                <div className="space-y-3">
                  {[
                    { name: 'Chidi O.',  text: 'Delivered same day. Legit product, fast service.' },
                    { name: 'Amaka N.', text: 'Best tech store in Nigeria. No cap.' },
                  ].map(r => (
                    <div key={r.name} className="bg-white rounded-xl border border-surface-200 p-3">
                      <p className="text-xs text-surface-700 leading-relaxed">"{r.text}"</p>
                      <p className="text-[10px] text-surface-400 mt-1.5 font-medium">— {r.name}</p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* ── NEWSLETTER ── */}
        <section className="border-t border-surface-100 bg-surface-50">
          <div className="page-container py-10">
            <div className="flex flex-col sm:flex-row items-center justify-between gap-6">
              <div>
                <h3 className="font-display text-xl font-bold text-surface-900">Stay in the loop.</h3>
                <p className="text-surface-500 text-sm mt-0.5">New arrivals, deals, and tech news — weekly.</p>
              </div>
              <div className="flex gap-2 w-full sm:w-auto">
                <input type="email" placeholder="your@email.com" className="input flex-1 sm:w-64 text-sm" />
                <button className="btn-primary flex-shrink-0">Subscribe</button>
              </div>
            </div>
          </div>
        </section>

      </main>
      <Footer />
    </div>
  )
}