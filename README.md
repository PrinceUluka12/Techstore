# TechStore 🛍️

A full-stack e-commerce platform for phones, tablets, smart watches, laptops, and accessories — built with React, .NET 8, and MySQL.

![License](https://img.shields.io/badge/license-MIT-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)
![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?logo=mysql)

---

## Overview

TechStore is a production-ready e-commerce application with two parts:

- **`techstore-frontend/`** — React storefront + admin dashboard
- **`TechStore.API/`** — ASP.NET Core REST API + MySQL database

---

## Features

### Storefront
- Product catalog with search, filter by category/price/brand, and sorting
- Product detail pages with add-to-cart and stock indicators
- Persistent shopping cart with a slide-out drawer
- Checkout with Canadian postal code validation and HST calculation
- Customer account — order history and order detail view
- User registration and JWT-based authentication

### Admin Dashboard
- KPI cards — revenue, orders, customers, low-stock alerts
- Revenue chart — daily revenue over the last 30 days
- Orders by status breakdown
- Top selling products
- Full product CRUD — create, edit, deactivate with image and pricing
- Order management — view details, update status through the full lifecycle
- Inventory management — restock, adjust stock, set low-stock thresholds
- Customer management — activate/deactivate accounts
- Date-range sales reports with revenue and units-sold charts

---

## Tech Stack

### Frontend
| | |
|---|---|
| Framework | React 18 + Vite 5 |
| Routing | React Router v6 |
| Styling | Tailwind CSS v3 |
| State | Zustand |
| HTTP | Axios |
| Forms | React Hook Form |
| Charts | Recharts |
| Fonts | Clash Display + DM Sans |

### Backend
| | |
|---|---|
| Runtime | .NET 8 / ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Database | MySQL 8+ (Pomelo driver) |
| Auth | JWT Bearer + BCrypt |
| Validation | FluentValidation |
| Docs | Swagger / OpenAPI |
| Architecture | Clean Architecture — Repository + Service pattern |

---

## Project Structure

```
Techstore/
├── TechStore.API/
│   ├── Controllers/         # Auth, Products, Cart, Orders, Inventory, Admin
│   ├── Data/                # EF Core DbContext + seeded categories
│   ├── DTOs/                # Request/response models for all features
│   ├── Helpers/             # JWT token generation
│   ├── Middleware/          # Global exception handler
│   ├── Models/              # User, Product, Order, Cart, Inventory
│   ├── Repositories/        # Data access layer (interfaces + implementations)
│   ├── Services/            # Business logic layer
│   ├── Validators/          # FluentValidation rules
│   ├── Program.cs           # DI, auth, CORS, Swagger setup
│   ├── appsettings.json     # Configuration
│   └── db-setup.sql         # MySQL database + user creation script
│
├── techstore-frontend/
│   ├── src/
│   │   ├── components/
│   │   │   ├── layout/      # Navbar, CartDrawer, Footer, AdminSidebar
│   │   │   ├── shop/        # ProductCard, ProductGrid
│   │   │   └── ui/          # Button, Modal, Input, Badge, Spinner, etc.
│   │   ├── pages/
│   │   │   ├── shop/        # Home, Products, ProductDetail, Auth, Orders
│   │   │   └── admin/       # Dashboard, Products, Orders, Inventory, Users, Reports
│   │   ├── services/        # Axios API client
│   │   ├── store/           # Zustand auth + cart + UI stores
│   │   ├── App.jsx          # Router + route guards
│   │   └── index.css        # Tailwind + component classes
│   ├── tailwind.config.js
│   ├── vite.config.js
│   └── package.json
│
└── README.md
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org)
- [MySQL 8+](https://dev.mysql.com/downloads/)

### 1. Clone the repo

```bash
git clone https://github.com/PrinceUluka12/Techstore.git
cd Techstore
```

### 2. Set up the database

```bash
mysql -u root -p < TechStore.API/db-setup.sql
```

This creates the `techstore_db` database and a `techstore_user` account.

### 3. Configure the backend

Edit `TechStore.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=techstore_db;User=techstore_user;Password=YourPassword123!;"
  },
  "Jwt": {
    "Secret": "REPLACE_WITH_A_STRONG_SECRET_AT_LEAST_32_CHARS",
    "Issuer": "TechStore.API",
    "Audience": "TechStore.Client",
    "ExpiryHours": "24"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

### 4. Run the backend

```bash
cd TechStore.API
dotnet ef migrations add InitialCreate
dotnet run
```

The API starts at `http://localhost:5000`. Swagger UI is available at the root URL.  
Tables are created automatically on first run.

### 5. Run the frontend

```bash
cd techstore-frontend
npm install
npm run dev
```

Opens at `http://localhost:5173`. The dev server proxies `/api` to `http://localhost:5000` automatically.

### 6. Create an admin account

Register a new account through the UI, then promote it in MySQL:

```sql
UPDATE techstore_db.Users SET Role = 'Admin' WHERE Email = 'your@email.com';
```

The `/admin` route and all admin endpoints will now be accessible.

---

## API Reference

### Auth
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register new account |
| POST | `/api/auth/login` | — | Login, returns JWT |
| GET | `/api/auth/me` | ✓ | Get own profile |
| PUT | `/api/auth/me` | ✓ | Update profile |

### Products
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/products` | — | Search & filter (query, categoryId, minPrice, maxPrice, brand, inStock, sortBy, page) |
| GET | `/api/products/featured` | — | Featured products |
| GET | `/api/products/categories` | — | All categories |
| GET | `/api/products/:id` | — | Product detail |
| POST | `/api/products` | Admin | Create product |
| PUT | `/api/products/:id` | Admin | Update product |
| DELETE | `/api/products/:id` | Admin | Deactivate product |

### Cart
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/cart` | ✓ | Get cart |
| POST | `/api/cart/items` | ✓ | Add item |
| PUT | `/api/cart/items/:id` | ✓ | Update quantity |
| DELETE | `/api/cart/items/:id` | ✓ | Remove item |
| DELETE | `/api/cart` | ✓ | Clear cart |

### Orders
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/orders/my` | ✓ | My order history |
| POST | `/api/orders/checkout` | ✓ | Place order from cart |
| GET | `/api/orders/:id` | ✓ | Order detail |
| GET | `/api/orders` | Admin | All orders |
| PUT | `/api/orders/:id/status` | Admin | Update order status |

### Inventory *(Admin)*
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/inventory/low-stock` | All low-stock items |
| POST | `/api/inventory/product/:id/restock` | Add stock |
| POST | `/api/inventory/product/:id/adjust` | Adjust stock (±) |
| PUT | `/api/inventory/product/:id/threshold` | Update alert threshold |

### Admin *(Admin)*
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/admin/dashboard` | KPIs + charts data |
| GET | `/api/admin/reports/sales` | Sales report (from/to params) |
| GET | `/api/admin/users` | Customer list |
| PUT | `/api/admin/users/:id/toggle` | Activate / deactivate user |

---

## Business Rules

- **Tax:** 13% Ontario HST applied at checkout
- **Shipping:** Free on orders over $100, otherwise $9.99
- **Inventory:** Stock is reserved when an order is placed and released on cancellation
- **Order number format:** `TS-YYYYMMDD-XXXXXX`
- **Soft delete:** Products are deactivated, not permanently deleted
- **Roles:** `Customer` (default) and `Admin`

---

## Environment Variables

For production, use environment variables instead of editing `appsettings.json`:

```bash
ConnectionStrings__DefaultConnection="Server=...;Database=techstore_db;..."
Jwt__Secret="your-production-secret-min-32-chars"
Jwt__ExpiryHours="12"
```

For the frontend, create a `.env` file:

```env
VITE_API_BASE_URL=https://your-api-domain.com/api
```

And update `src/services/api.js` to use `import.meta.env.VITE_API_BASE_URL`.

---

## Database Migrations

```bash
cd TechStore.API

# Create a new migration after model changes
dotnet ef migrations add <MigrationName>

# Apply to database
dotnet ef database update

# Rollback to a previous migration
dotnet ef database update <PreviousMigrationName>
```

---

## License

MIT — free to use, modify, and distribute.
