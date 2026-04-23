# TechStore API — .NET 8 + MySQL Backend

Full-stack e-commerce REST API for phones, tablets, smart watches and accessories.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 / ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Database | MySQL 8+ via Pomelo EF Core driver |
| Auth | JWT Bearer + BCrypt password hashing |
| Mapping | AutoMapper 12 |
| Validation | FluentValidation 11 |
| API Docs | Swagger / OpenAPI (Swashbuckle) |
| Architecture | Clean Architecture — Repository + Service pattern |

---

## Project Structure

```
TechStore.API/
├── Controllers/
│   └── Controllers.cs          # Auth, Products, Cart, Orders, Inventory, Admin
├── Data/
│   └── AppDbContext.cs          # EF Core DbContext + seeded categories
├── DTOs/
│   ├── Auth/                    # RegisterRequest, LoginRequest, AuthResponse
│   ├── Product/                 # ProductDto, CreateProductRequest, SearchParams
│   ├── Order/                   # OrderDto, CreateOrderRequest, UpdateStatusRequest
│   ├── Cart/                    # CartDto, AddToCartRequest
│   ├── Inventory/               # InventoryDto, AdjustStockRequest, RestockRequest
│   └── Admin/                   # DashboardStatsDto, SalesReportDto, UserAdminDto
├── Helpers/
│   └── JwtHelper.cs             # Token generation + validation
├── Middleware/
│   └── ExceptionMiddleware.cs   # Global error handling → consistent JSON errors
├── Models/
│   ├── User.cs
│   ├── Product.cs               # Product + Category
│   ├── Order.cs                 # Order + OrderItem + enums
│   └── Cart.cs                  # Cart + CartItem + Inventory
├── Repositories/
│   ├── Interfaces/IRepositories.cs
│   └── Repositories.cs
├── Services/
│   ├── Interfaces/IServices.cs
│   ├── AuthService.cs
│   ├── ProductService.cs
│   ├── CartService.cs
│   ├── OrderService.cs
│   ├── InventoryService.cs
│   └── AdminService.cs
├── Validators/
│   └── Validators.cs            # FluentValidation rules
├── Program.cs                   # DI, middleware pipeline, JWT, CORS, Swagger
├── appsettings.json
└── db-setup.sql                 # MySQL user + database creation script
```

---

## Quick Start

### 1. MySQL Setup

```bash
mysql -u root -p < db-setup.sql
```

### 2. Configure `appsettings.json`

Update the connection string and JWT secret:

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
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  }
}
```

### 3. Restore + Run

```bash
dotnet restore
dotnet ef migrations add InitialCreate    # First time only
dotnet run
```

Tables are auto-created on startup. Swagger UI opens at `http://localhost:5000`.

### 4. Create Admin User

Register normally via `POST /api/auth/register`, then promote in MySQL:

```sql
UPDATE techstore_db.Users SET Role = 'Admin' WHERE Email = 'your@email.com';
```

---

## API Reference

### Auth — `/api/auth`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/register` | — | Register new customer |
| POST | `/login` | — | Login, returns JWT |
| GET | `/me` | ✓ | Get own profile |
| PUT | `/me` | ✓ | Update profile |

### Products — `/api/products`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | — | Search + filter products |
| GET | `/featured` | — | Featured products |
| GET | `/categories` | — | All categories |
| GET | `/category/{id}` | — | Products by category |
| GET | `/{id}` | — | Product detail |
| POST | `/` | Admin | Create product |
| PUT | `/{id}` | Admin | Update product |
| DELETE | `/{id}` | Admin | Deactivate product |

**Search query params:** `query`, `categoryId`, `minPrice`, `maxPrice`, `brand`, `inStock`, `sortBy`, `sortDir`, `page`, `pageSize`

### Cart — `/api/cart`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/` | ✓ | Get user's cart |
| POST | `/items` | ✓ | Add item |
| PUT | `/items/{itemId}` | ✓ | Update quantity |
| DELETE | `/items/{itemId}` | ✓ | Remove item |
| DELETE | `/` | ✓ | Clear cart |

### Orders — `/api/orders`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/my` | ✓ | My order history |
| POST | `/checkout` | ✓ | Place order from cart |
| GET | `/{id}` | ✓ | Get order detail |
| GET | `/` | Admin | All orders (filterable by status) |
| PUT | `/{id}/status` | Admin | Update order status |

**Order statuses:** `Pending → Confirmed → Processing → Shipped → Delivered` or `Cancelled / Refunded`

### Inventory — `/api/inventory` *(Admin only)*

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/product/{productId}` | Get inventory for product |
| GET | `/low-stock` | All low-stock items |
| POST | `/product/{productId}/adjust` | Adjust stock (±) |
| POST | `/product/{productId}/restock` | Add stock + update timestamp |
| PUT | `/product/{productId}/threshold` | Update low-stock threshold |

### Admin — `/api/admin` *(Admin only)*

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/dashboard` | KPIs: revenue, orders, customers, top products |
| GET | `/reports/sales?from=&to=` | Sales report for date range |
| GET | `/users` | Paginated user list with spend totals |
| PUT | `/users/{userId}/toggle` | Activate / deactivate user |

---

## Business Rules

- **Tax:** 7.5% VAT (Nigeria) applied automatically at checkout
- **Free shipping:** Orders over ₦100 ship free; otherwise ₦9.99
- **Inventory reservation:** Stock reserved when order is placed; released on cancellation
- **Soft delete:** Products are deactivated, not deleted
- **Order number format:** `TS-YYYYMMDD-XXXXXX`

---

## EF Core Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName>

# Apply to database
dotnet ef database update

# Rollback
dotnet ef database update <PreviousMigrationName>
```

---

## Environment Variables (Production)

Use environment variables or Docker secrets instead of appsettings for production:

```bash
ConnectionStrings__DefaultConnection="..."
Jwt__Secret="your-production-secret"
```