import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, Smartphone, Tablet, Watch, Laptop, Zap, Shield, Truck } from 'lucide-react'
import { productApi } from '../../services/api'
import { ProductGrid } from '../../components/shop/ProductCard'
import { Navbar, CartDrawer, Footer } from '../../components/layout/ShopLayout'
import { useCartStore } from '../../store'
import { cartApi } from '../../services/api'
import { useAuthStore } from '../../store'

const categories = [
  { id: 1, name: 'Smartphones', icon: Smartphone, color: 'from-blue-500 to-brand-600', desc: 'Latest flagships' },
  { id: 2, name: 'Tablets',     icon: Tablet,     color: 'from-violet-500 to-purple-600', desc: 'Work & play' },
  { id: 3, name: 'Smart Watches', icon: Watch,    color: 'from-emerald-500 to-teal-600', desc: 'Stay connected' },
  { id: 4, name: 'Laptops',     icon: Laptop,     color: 'from-orange-500 to-red-500', desc: 'Power & portability' },
]

const perks = [
  { icon: Truck,   title: 'Free Shipping', desc: 'On orders over $100' },
  { icon: Shield,  title: '2-Year Warranty', desc: 'On all devices' },
  { icon: Zap,     title: 'Same-Day Dispatch', desc: 'Order before 2PM' },
]

export default function HomePage() {
  const [featured, setFeatured] = useState([])
  const [loading, setLoading] = useState(true)
  const { setCart } = useCartStore()
  const { isAuthenticated } = useAuthStore()

  useEffect(() => {
    productApi.featured().then(r => setFeatured(r.data)).finally(() => setLoading(false))
    if (isAuthenticated) {
      cartApi.get().then(r => setCart(r.data)).catch(() => {})
    }
  }, [isAuthenticated])

  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <CartDrawer />
      <main className="flex-1">

        {/* ── Hero ── */}
        <section className="relative bg-surface-950 text-white overflow-hidden">
          {/* Background grid */}
          <div className="absolute inset-0 opacity-[0.03]"
            style={{ backgroundImage: 'linear-gradient(rgba(255,255,255,.1) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,.1) 1px, transparent 1px)', backgroundSize: '48px 48px' }} />
          {/* Gradient blobs */}
          <div className="absolute top-0 left-1/4 w-96 h-96 bg-brand-600 rounded-full blur-[120px] opacity-20" />
          <div className="absolute bottom-0 right-1/4 w-64 h-64 bg-violet-600 rounded-full blur-[100px] opacity-15" />

          <div className="page-container relative py-24 sm:py-32">
            <div className="max-w-3xl animate-fade-up">
              <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-white/10 text-xs font-medium mb-6 border border-white/10">
                <span className="w-1.5 h-1.5 bg-emerald-400 rounded-full animate-pulse" />
                New arrivals just dropped
              </div>
              <h1 className="font-display text-5xl sm:text-7xl font-bold leading-none tracking-tight mb-6">
                Premium Tech,<br />
                <span className="text-brand-400">Delivered Fast.</span>
              </h1>
              <p className="text-surface-300 text-lg sm:text-xl mb-10 max-w-xl text-balance">
                Shop the latest smartphones, tablets, smart watches, and accessories from the world's top brands.
              </p>
              <div className="flex flex-wrap gap-3">
                <Link to="/products" className="btn-primary btn-lg">
                  Shop Now <ArrowRight className="w-4 h-4" />
                </Link>
                <Link to="/products?featured=true" className="btn bg-white/10 text-white border border-white/20 px-7 py-3.5 text-base hover:bg-white/20">
                  View Deals
                </Link>
              </div>
            </div>
          </div>
        </section>

        {/* ── Perks bar ── */}
        <section className="border-y border-surface-100 bg-surface-50">
          <div className="page-container py-5">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              {perks.map(p => (
                <div key={p.title} className="flex items-center gap-3 animate-fade-up">
                  <div className="w-9 h-9 rounded-xl bg-brand-50 flex items-center justify-center">
                    <p.icon className="w-4 h-4 text-brand-600" />
                  </div>
                  <div>
                    <p className="text-sm font-semibold text-surface-900">{p.title}</p>
                    <p className="text-xs text-surface-500">{p.desc}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>

        {/* ── Categories ── */}
        <section className="page-container py-16">
          <div className="flex items-center justify-between mb-8">
            <h2 className="section-title">Shop by Category</h2>
            <Link to="/products" className="btn-ghost text-brand-600 hover:text-brand-700">
              All Products <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 animate-stagger">
            {categories.map(cat => (
              <Link key={cat.id} to={`/products?categoryId=${cat.id}`}
                className="group relative rounded-2xl overflow-hidden aspect-[4/3] cursor-pointer">
                <div className={`absolute inset-0 bg-gradient-to-br ${cat.color} opacity-90 group-hover:opacity-100 transition-opacity`} />
                <div className="absolute inset-0 flex flex-col items-center justify-center text-white p-4">
                  <cat.icon className="w-10 h-10 mb-3 group-hover:scale-110 transition-transform" />
                  <p className="font-display font-semibold text-base">{cat.name}</p>
                  <p className="text-xs text-white/70 mt-0.5">{cat.desc}</p>
                </div>
              </Link>
            ))}
          </div>
        </section>

        {/* ── Featured products ── */}
        <section className="page-container pb-16">
          <div className="flex items-center justify-between mb-8">
            <h2 className="section-title">Featured Products</h2>
            <Link to="/products" className="btn-ghost text-brand-600 hover:text-brand-700">
              View All <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
          <ProductGrid products={featured} loading={loading} />
        </section>

        {/* ── CTA banner ── */}
        <section className="page-container pb-16">
          <div className="rounded-3xl bg-gradient-to-r from-brand-600 to-brand-500 p-10 sm:p-16 text-white relative overflow-hidden">
            <div className="absolute right-0 top-0 w-72 h-72 bg-white/5 rounded-full -translate-y-1/2 translate-x-1/4" />
            <div className="relative">
              <p className="text-brand-200 text-sm font-medium mb-3">Limited Time</p>
              <h2 className="font-display text-3xl sm:text-4xl font-bold mb-4">Up to 40% off<br />last season's tech</h2>
              <Link to="/products?sortBy=price&sortDir=asc" className="btn bg-white text-brand-600 hover:bg-brand-50 px-6 py-3 font-semibold">
                Shop Deals <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
          </div>
        </section>

      </main>
      <Footer />
    </div>
  )
}
