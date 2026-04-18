# TechStore Frontend — React + Vite + Tailwind

Full-stack e-commerce storefront + admin dashboard for phones, tablets, smart watches and accessories.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | React 18 + Vite 5 |
| Routing | React Router v6 |
| Styling | Tailwind CSS v3 (custom design system) |
| State | Zustand (auth + cart + UI) |
| HTTP | Axios (auto JWT injection, 401 redirect) |
| Forms | React Hook Form |
| Charts | Recharts |
| Notifications | react-hot-toast |
| Icons | Lucide React |
| Fonts | Clash Display (headings) + DM Sans (body) |

---

## Project Structure

```
src/
├── components/
│   ├── layout/
│   │   ├── ShopLayout.jsx      # Navbar, CartDrawer, Footer
│   │   └── AdminLayout.jsx     # Sidebar, top bar, mobile nav
│   ├── shop/
│   │   └── ProductCard.jsx     # ProductCard + ProductGrid (skeleton loading)
│   └── ui/
│       └── index.jsx           # Spinner, Modal, Input, Select, Badge,
│                               # StatusBadge, Price, Pagination, StatCard
├── pages/
│   ├── shop/
│   │   ├── HomePage.jsx        # Hero, categories, featured products, CTA
│   │   ├── ProductsPage.jsx    # Search, filter, sort, category pills
│   │   ├── ProductDetailPage.jsx
│   │   ├── AuthPages.jsx       # Login, Register, Checkout
│   │   └── OrdersPage.jsx      # My Orders, Order Detail
│   └── admin/
│       ├── AdminDashboard.jsx  # KPIs, revenue chart, top products, order status chart
│       ├── AdminProducts.jsx   # CRUD product management
│       ├── AdminOrders.jsx     # View & update order status
│       ├── AdminInventory.jsx  # Stock adjust, restock, thresholds
│       └── AdminUsersReports.jsx # User management + date-range sales reports
├── services/
│   └── api.js                  # Axios client + all API calls
├── store/
│   └── index.js                # Zustand: useAuthStore, useCartStore, useUIStore
├── App.jsx                     # Router, route guards (RequireAuth, RequireAdmin, GuestOnly)
├── main.jsx
└── index.css                   # Tailwind + custom component classes
```

---

## Quick Start

```bash
# 1. Install dependencies
npm install

# 2. Start dev server (API must be running on :5000)
npm run dev
# Opens at http://localhost:5173

# 3. Production build
npm run build
```

The Vite dev server proxies `/api` → `http://localhost:5000` automatically.

---

## Pages & Routes

### Storefront
| Route | Page | Auth |
|---|---|---|
| `/` | Homepage — hero, categories, featured | — |
| `/products` | Product listing — search, filter, sort | — |
| `/products/:id` | Product detail, add to cart | — |
| `/login` | Sign in | Guest only |
| `/register` | Create account | Guest only |
| `/checkout` | Checkout form | ✓ Required |
| `/account/orders` | My order history | ✓ Required |
| `/account/orders/:id` | Order detail | ✓ Required |

### Admin (Admin role required)
| Route | Page |
|---|---|
| `/admin` | Dashboard — KPIs, revenue chart, top products |
| `/admin/products` | Product CRUD with image, pricing, stock |
| `/admin/orders` | Order list, status updates, detail modal |
| `/admin/inventory` | Stock levels, restock, adjust, thresholds |
| `/admin/users` | Customer list, activate/deactivate |
| `/admin/reports` | Date-range sales reports, revenue chart |

---

## Auth Flow

1. User registers or logs in → receives JWT from backend
2. Token stored in `localStorage` + Zustand `useAuthStore`
3. Axios interceptor attaches `Authorization: Bearer <token>` to all requests
4. On 401 → token cleared, redirected to `/login`
5. Route guards: `RequireAuth`, `RequireAdmin`, `GuestOnly`

---

## Design System

Built with Tailwind CSS using a custom design token system:

- **Fonts:** Clash Display (headings) + DM Sans (body) + JetBrains Mono
- **Brand:** Blue `#2d52ff` with surface grays
- **Components via @layer components:** `.btn-primary`, `.btn-secondary`, `.card`, `.card-hover`, `.input`, `.label`, `.badge-*`, `.nav-item`, `.stat-card`, `.table-container`, `.data-table`
- **Animations:** `animate-fade-up`, `animate-fade-in`, `animate-slide-in`, `animate-stagger` (staggered children)

---

## Connecting to a Different Backend

Update the proxy in `vite.config.js`:

```js
proxy: {
  '/api': {
    target: 'http://your-api-host:5000',
    changeOrigin: true,
  }
}
```

For production, set `VITE_API_BASE_URL` and update `src/services/api.js`:

```js
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
})
```
