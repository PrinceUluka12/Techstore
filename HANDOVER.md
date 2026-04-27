# Hytel Phones — Application Handover Document

> **Last updated:** April 2026  
> **Prepared for:** Technical handover / onboarding

---

## Table of Contents

1. [Application Overview](#1-application-overview)
2. [Architecture](#2-architecture)
3. [Azure Infrastructure](#3-azure-infrastructure)
4. [Local Development Setup](#4-local-development-setup)
5. [Environment Configuration](#5-environment-configuration)
6. [Azure Key Vault Secrets](#6-azure-key-vault-secrets)
7. [Database](#7-database)
8. [Authentication & Security](#8-authentication--security)
9. [Email — Azure Communication Services](#9-email--azure-communication-services)
10. [Payments — Paystack](#10-payments--paystack)
11. [Image Storage — Azure Blob](#11-image-storage--azure-blob)
12. [Frontend Routes & Pages](#12-frontend-routes--pages)
13. [Backend API Endpoints](#13-backend-api-endpoints)
14. [Deployment](#14-deployment)
15. [Pending / Recommended Next Steps](#15-pending--recommended-next-steps)

---

## 1. Application Overview

**Hytel Phones** is a Nigerian e-commerce platform selling smartphones, tablets, laptops, and smartwatches.

| Item | Detail |
|---|---|
| Live site | https://www.pgusolutions.com |
| Admin panel | https://www.pgusolutions.com/admin |
| API (Swagger) | https://hytelphonesapi-fkamcxexbwdwccc6.canadacentral-01.azurewebsites.net/index.html |
| Support email | support@hytelphones.ng |
| Sender email | DoNotReply@pgusolutions.com |

---

## 2. Architecture

```
┌─────────────────────────┐      ┌──────────────────────────────┐
│  React + Vite (SPA)     │ ───> │  .NET 8 ASP.NET Core Web API │
│  Azure Static Web Apps  │      │  Azure App Service           │
└─────────────────────────┘      └──────────┬───────────────────┘
                                            │
              ┌─────────────────────────────┼──────────────────────────┐
              │                             │                          │
   ┌──────────▼──────────┐   ┌─────────────▼──────┐   ┌──────────────▼──────┐
   │  Azure SQL Database │   │  Azure Blob Storage │   │  Azure Key Vault    │
   │  (techstore_db)     │   │  (hytelimages)      │   │  (hytel-keyvault)   │
   └─────────────────────┘   └────────────────────┘   └─────────────────────┘
                                            │
              ┌─────────────────────────────┼──────────────────────────┐
              │                             │                          │
   ┌──────────▼──────────┐   ┌─────────────▼──────┐
   │  Azure Communication│   │  Paystack            │
   │  Services (Email)   │   │  (Payment gateway)  │
   └─────────────────────┘   └─────────────────────┘
```

### Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 18, Vite 5, Tailwind CSS 3, Zustand, React Router v6 |
| Backend | .NET 8, ASP.NET Core Web API, Entity Framework Core 8 |
| Database | SQL Server (LocalDB for dev, Azure SQL for prod) |
| ORM | EF Core 8 with code-first migrations |
| Auth | JWT access tokens (15 min) + HTTP-only refresh tokens (7 days) |
| Email | Azure Communication Services |
| Payments | Paystack (inline popup + webhook) |
| Image storage | Azure Blob Storage + Azure CDN Front Door |
| Secrets | Azure Key Vault (production), appsettings.Development.json (local) |
| Validation | FluentValidation |
| Rate limiting | ASP.NET Core built-in rate limiter (10 req/min on auth endpoints) |

---

## 3. Azure Infrastructure

| Resource | Name | Purpose |
|---|---|---|
| Static Web App | pgusolutions | Frontend hosting + custom domain |
| App Service | (API host) | Backend API hosting |
| Azure SQL | techstore_db | Production database |
| Storage Account | hytelimages | Product/category image uploads |
| Azure CDN Front Door | hytelimages-e2fge0bsabapfnd7.z03.azurefd.net | Image CDN delivery |
| Communication Services | hytelcomunication | Transactional email sending |
| Key Vault | hytel-keyvault | All production secrets |

### App Service — Required Settings

The App Service must have a **System-assigned Managed Identity** enabled, with the following role on the Key Vault:

```
Role:  Key Vault Secrets User
Scope: hytel-keyvault
```

To set this via CLI:
```bash
az keyvault set-policy \
  --name hytel-keyvault \
  --object-id <app-service-principal-id> \
  --secret-permissions get list
```

---

## 4. Local Development Setup

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- SQL Server LocalDB (ships with Visual Studio) or SQL Server Express
- Azure CLI (for Key Vault / storage access if needed)
- Git

### Clone and run

```bash
git clone <repo-url>
cd Techstore
```

**Backend:**
```bash
cd TechStore.API
dotnet restore
dotnet run
# API available at https://localhost:7214
# Swagger UI at https://localhost:7214/index.html
```

**Frontend:**
```bash
cd techstore-frontend
npm install
npm run dev
# App available at http://localhost:5173
```

The backend auto-migrates the database on startup. No manual migration step needed after pulling new code.

---

## 5. Environment Configuration

### Config file strategy

| File | Committed | Purpose |
|---|---|---|
| `appsettings.json` | ✅ Yes | Structure only — all secret values are empty strings |
| `appsettings.Development.json` | ❌ No (.gitignored) | Real keys for local dev |
| Azure Key Vault | — | All secrets for production |

### appsettings.Development.json (local dev template)

This file must be created manually by each developer. It is **gitignored** and never committed.

```json
{
  "KeyVault": {
    "Url": "https://hytel-keyvault.vault.azure.net/"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=techstore_db;Integrated Security=True;TrustServerCertificate=True;"
  },
  "AzureStorage": {
    "ConnectionString": "<azure-storage-connection-string>",
    "AccountName": "hytelimages",
    "ContainerName": "images",
    "CdnBaseUrl": "https://hytelimages-e2fge0bsabapfnd7.z03.azurefd.net"
  },
  "Jwt": {
    "Secret": "<jwt-secret>",
    "Issuer": "TechStore.API",
    "Audience": "TechStore.Client",
    "AccessTokenExpiryMinutes": "15",
    "RefreshTokenExpiryDays": "7"
  },
  "Auth": {
    "MaxFailedAttempts": "5",
    "LockoutMinutes": "15"
  },
  "Email": {
    "ConnectionString": "<acs-connection-string>",
    "From": "DoNotReply@pgusolutions.com",
    "FromName": "Hytel Phones",
    "PasswordResetBaseUrl": "http://localhost:5173/reset-password"
  },
  "Paystack": {
    "PublicKey": "<paystack-public-key>",
    "SecretKey": "<paystack-secret-key>",
    "WebhookSecret": "<paystack-webhook-secret>"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173"
    ]
  }
}
```

> Retrieve actual values from the Azure Key Vault or from the project owner.

---

## 6. Azure Key Vault Secrets

Key Vault: `https://hytel-keyvault.vault.azure.net/`

.NET's Key Vault config provider maps `--` in secret names to `:` in config keys.

| Secret Name | Maps to config key | Description |
|---|---|---|
| `Jwt--Secret` | `Jwt:Secret` | JWT signing key |
| `ConnectionStrings--DefaultConnection` | `ConnectionStrings:DefaultConnection` | Azure SQL connection string |
| `AzureStorage--ConnectionString` | `AzureStorage:ConnectionString` | Blob storage connection string |
| `Email--ConnectionString` | `Email:ConnectionString` | ACS email connection string |
| `Email--From` | `Email:From` | Sender address |
| `Paystack--PublicKey` | `Paystack:PublicKey` | Paystack public key |
| `Paystack--SecretKey` | `Paystack:SecretKey` | Paystack secret key |

> **Note:** `Paystack--WebhookSecret` has not been added yet. Add it once Paystack webhook signing is configured in the Paystack dashboard.

To add or update a secret:
```bash
az keyvault secret set \
  --vault-name hytel-keyvault \
  --name "Secret--Name" \
  --value "the-value"
```

### How Key Vault is loaded

In `Program.cs`, Key Vault is loaded **only in non-Development environments**:

```csharp
if (!string.IsNullOrEmpty(keyVaultUrl) && !builder.Environment.IsDevelopment())
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}
```

This means local dev uses `appsettings.Development.json`; production uses Key Vault.

---

## 7. Database

### Connection

- **Local:** SQL Server LocalDB — `Server=(localdb)\MSSQLLocalDB;Database=techstore_db`
- **Production:** Azure SQL — connection string stored in Key Vault as `ConnectionStrings--DefaultConnection`

### Migrations

All migrations are code-first via EF Core. The app **auto-migrates on startup** (`db.Database.MigrateAsync()`), so deploying new code automatically applies pending migrations.

| Migration | Description |
|---|---|
| `InitialCreate` | Core schema: Users, Products, Categories, Orders, Cart, Inventory |
| `AddRefreshTokens` | JWT refresh token table |
| `AddCouponsAndOrderDiscount` | Coupons table and coupon fields on orders |
| `AddReviewSystem` | Product reviews |
| `AddOrderStatusLog` | Order status history timeline |
| `AddUserLockout` | Brute-force lockout fields on Users |
| `AddPasswordResetAndWishlist` | Password reset token fields + Wishlist table |
| `AddOrderCouponFields` | DiscountAmount + CouponCode on Orders |

To run migrations manually:
```bash
cd TechStore.API
dotnet ef database update
```

To add a new migration:
```bash
dotnet ef migrations add <MigrationName>
```

### Core Models

| Model | Description |
|---|---|
| `User` | Customer/admin accounts with lockout, password reset, wishlist |
| `Product` | Products with category, price, stock, images |
| `Order` | Orders with status, payment status, coupon discount, shipping address |
| `OrderStatusLog` | Immutable history of every order status change |
| `Cart` / `CartItem` | Per-user shopping cart |
| `Coupon` | Discount codes with expiry, min order, usage limits |
| `Review` | Product reviews (rating + comment) |
| `Wishlist` | Saved products per user |
| `RefreshToken` | JWT refresh tokens with reuse detection |

---

## 8. Authentication & Security

### Flow

1. `POST /api/auth/register` or `POST /api/auth/login` → returns `accessToken` (JWT, 15 min) + sets `refreshToken` cookie (HTTP-only, 7 days)
2. All protected requests send the `Authorization: Bearer <accessToken>` header
3. When the access token expires, the frontend calls `POST /api/auth/refresh` with the cookie → new token pair issued
4. On password change or logout-everywhere, all refresh tokens for that user are revoked

### Roles

| Role | Access |
|---|---|
| `Customer` | All shop endpoints, own orders and wishlist, profile |
| `Admin` | All customer access + admin panel (products, inventory, orders, users, coupons, images) |

### Security features

- **Brute-force protection:** 5 failed logins → 15-minute account lockout (configurable via `Auth:MaxFailedAttempts` / `Auth:LockoutMinutes`)
- **Refresh token rotation:** each refresh issues a new token and revokes the old one
- **Reuse detection:** presenting a previously-revoked refresh token immediately revokes all sessions for that user
- **Password reset tokens:** GUID-based, URL-safe Base64, 1-hour expiry, cleared after use
- **Rate limiting:** auth endpoints limited to 10 requests/minute
- **CORS:** only `pgusolutions.com` and localhost origins allowed

### To promote a user to Admin

There is no UI for this. Run directly against the database or via Swagger:

```
POST /api/auth/promote/{userId}
Authorization: Bearer <admin-token>
```

---

## 9. Email — Azure Communication Services

### Resource

- **Resource:** `hytelcomunication.unitedstates.communication.azure.com`
- **Sender address:** `DoNotReply@pgusolutions.com`
- **Verified domain:** `pgusolutions.com` (configured in ACS → Email → Domains)

### Emails sent automatically

| Trigger | Subject |
|---|---|
| New user registration | Welcome to Hytel Phones |
| Order placed | Order Confirmed — {orderNumber} |
| Order status updated (admin) | Order Update — {orderNumber} |
| Forgot password | Reset Your Password |

### Password reset flow

1. User submits email at `/forgot-password`
2. Backend generates a URL-safe Base64 token, saves with 1-hour expiry
3. Email sent with link: `{PasswordResetBaseUrl}?token={token}`
4. User clicks link → `/reset-password?token=...` → submits new password
5. Token is cleared, all sessions revoked

### If emails stop working

1. Check the API logs for `SendGrid returned` or `Failed to send email` messages
2. Verify the ACS connection string is still valid in Key Vault
3. Confirm `pgusolutions.com` domain is still verified in the ACS portal (Settings → Email → Domains)

---

## 10. Payments — Paystack

### Keys

- **Public key** (frontend): stored in `appsettings.Development.json` locally; Key Vault in production
- **Secret key** (backend verify): Key Vault — `Paystack--SecretKey`
- **Webhook secret:** ⚠️ Not yet configured — see below

### Payment flow

```
1. Customer fills checkout form
2. Frontend creates order (POST /api/orders) → status: Pending
3. Paystack popup opens with order.id in metadata and order.total as amount
4. Customer completes payment on Paystack
5. On success: frontend calls POST /api/payments/paystack/verify/{reference}
6. Backend calls Paystack API to verify → sets order status: Confirmed, payment: Paid
7. Confirmation email sent to customer
```

### Webhook (backup flow)

The webhook handles cases where the browser closes before step 5 completes. Paystack calls `POST /api/payments/paystack/webhook` directly.

**⚠️ Action required:** Register the webhook URL in the Paystack dashboard and add the webhook secret to Key Vault:

1. Paystack Dashboard → Settings → Webhooks → add URL: `https://<api-url>/api/payments/paystack/webhook`
2. Copy the webhook secret shown by Paystack
3. Add to Key Vault:
   ```bash
   az keyvault secret set --vault-name hytel-keyvault --name "Paystack--WebhookSecret" --value "<secret>"
   ```

### Test vs Live keys

The application is currently configured with **live Paystack keys**. Any successful payment is a real charge. To test safely:

1. Log into Paystack dashboard → toggle to Test mode
2. Replace `Paystack:SecretKey` and `Paystack:PublicKey` in `appsettings.Development.json` with test keys
3. Use Paystack test card: `4084 0840 8408 4081`, any future expiry, any CVV, OTP: `123456`

---

## 11. Image Storage — Azure Blob

### Configuration

| Setting | Value |
|---|---|
| Storage account | `hytelimages` |
| Container | `images` |
| CDN URL | `https://hytelimages-e2fge0bsabapfnd7.z03.azurefd.net` |

All product and category images are uploaded via the admin **Images** page (`/admin/images`). The upload goes to Blob Storage and the returned CDN URL is saved against the product.

Images are served through Azure Front Door CDN — do not use the raw blob storage URL in the app.

---

## 12. Frontend Routes & Pages

### Storefront (public)

| Route | Page | Notes |
|---|---|---|
| `/` | HomePage | Hero, categories, featured products, perks |
| `/products` | ProductsPage | Search, filter, sort, pagination |
| `/products/:id` | ProductDetailPage | Images, reviews, add to cart, wishlist |
| `/login` | LoginPage | JWT auth, guest-only |
| `/register` | RegisterPage | Creates account, sends welcome email |
| `/forgot-password` | ForgotPasswordPage | Triggers reset email |
| `/reset-password?token=` | ResetPasswordPage | Validates token, sets new password |

### Customer account (requires login)

| Route | Page |
|---|---|
| `/checkout` | CheckoutPage — shipping form, Paystack payment, coupon code |
| `/account/orders` | MyOrdersPage |
| `/account/orders/:id` | OrderDetailPage — status timeline, items, totals |
| `/account/profile` | AccountProfilePage — edit name/phone/address, change password |
| `/account/wishlist` | WishlistPage |

### Admin panel (requires Admin role)

| Route | Page |
|---|---|
| `/admin` | Dashboard — revenue, orders, stats |
| `/admin/products` | Product CRUD |
| `/admin/inventory` | Stock management |
| `/admin/images` | Image upload and CDN management |
| `/admin/orders` | Order management, status updates |
| `/admin/users` | Customer list |
| `/admin/coupons` | Coupon CRUD |
| `/admin/reports` | Sales reports and charts |

### SPA routing on Azure Static Web Apps

The file `techstore-frontend/public/staticwebapp.config.json` configures the navigation fallback so that all routes resolve to `index.html` instead of returning 404.

---

## 13. Backend API Endpoints

Base path: `/api`

### Auth — `/api/auth`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/register` | — | Create account |
| POST | `/login` | — | Login, returns tokens |
| POST | `/refresh` | — | Refresh access token |
| POST | `/logout` | User | Revoke current session |
| POST | `/logout-everywhere` | User | Revoke all sessions |
| POST | `/change-password` | User | Change password |
| POST | `/forgot-password` | — | Send reset email |
| POST | `/reset-password` | — | Apply new password via token |
| GET | `/profile` | User | Get own profile |
| PUT | `/profile` | User | Update profile |
| POST | `/promote/{id}` | Admin | Promote user to Admin |

### Products — `/api/products`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/search` | — | Search/filter/sort with pagination |
| GET | `/featured` | — | Featured product list |
| GET | `/categories` | — | All categories |
| GET | `/{id}` | — | Single product detail |
| POST | `/` | Admin | Create product |
| PUT | `/{id}` | Admin | Update product |
| DELETE | `/{id}` | Admin | Delete product |

### Orders — `/api/orders`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/checkout` | User | Create order from cart |
| GET | `/my` | User | Own orders |
| GET | `/{id}` | User/Admin | Order detail |
| GET | `/{id}/history` | User/Admin | Status timeline |
| GET | `/` | Admin | All orders |
| PUT | `/{id}/status` | Admin | Update order status |

### Payments — `/api/payments/paystack`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/verify/{reference}` | User | Verify payment and confirm order |
| POST | `/webhook` | — | Paystack webhook (HMAC-validated) |

### Coupons — `/api/coupons`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/validate` | User | Validate coupon code against a subtotal |
| GET | `/` | Admin | List all coupons |
| POST | `/` | Admin | Create coupon |
| PUT | `/{id}` | Admin | Update coupon |
| DELETE | `/{id}` | Admin | Delete coupon |

### Wishlist — `/api/wishlist`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | User | Get wishlist |
| POST | `/` | User | Add product |
| DELETE | `/{productId}` | User | Remove product |
| GET | `/check/{productId}` | User | Check if product is in wishlist |

### Other endpoints

- `GET /api/cart`, `POST /api/cart/items`, `PUT /api/cart/items/{id}`, `DELETE /api/cart/items/{id}` — Cart management
- `GET /api/inventory`, `PUT /api/inventory/{id}` — Inventory (Admin)
- `POST /api/images/upload`, `DELETE /api/images` — Image management (Admin)
- `GET /api/admin/dashboard`, `GET /api/admin/reports` — Dashboard stats (Admin)
- `GET /api/products/{id}/reviews`, `POST /api/products/{id}/reviews` — Reviews

---

## 14. Deployment

### Backend — Azure App Service

1. Publish the .NET 8 project:
   ```bash
   cd TechStore.API
   dotnet publish -c Release -o ./publish
   ```
2. Deploy the `publish/` folder to the App Service (via Azure DevOps, GitHub Actions, or VS Publish)
3. Set `ASPNETCORE_ENVIRONMENT=Production` in the App Service → Configuration → Application settings
4. Ensure Managed Identity is enabled and has Key Vault access (see section 3)
5. Database migrations run automatically on startup

### Frontend — Azure Static Web Apps

1. Build the frontend:
   ```bash
   cd techstore-frontend
   npm run build
   ```
2. Deploy the `dist/` folder to Azure Static Web Apps
3. The `staticwebapp.config.json` in `public/` is automatically included in the build and handles SPA routing

### CORS

When deploying to a new domain, add it to `appsettings.json` → `Cors:AllowedOrigins` and redeploy:

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173",
    "https://icy-dune-0dc0e440f.7.azurestaticapps.net",
    "https://www.pgusolutions.com"
  ]
}
```

---

## 15. Pending / Recommended Next Steps

### ⚠️ Must do before going fully live

| # | Task | Why |
|---|---|---|
| 1 | **Register Paystack webhook URL** in the Paystack dashboard and add `Paystack--WebhookSecret` to Key Vault | Without this, payments that succeed but don't call the verify endpoint (e.g. browser crash) will stay as Pending orders |
| 2 | **Verify Paystack live keys are correct** — test with a small real payment end-to-end | Confirms the full payment → verify → order confirmed → email flow |
| 3 | **Set production DB connection string** in Key Vault (`ConnectionStrings--DefaultConnection`) | The Key Vault entry exists but must point to the Azure SQL instance |
| 4 | **Confirm App Service Managed Identity** has Key Vault Secrets User role | Without this the API will fail to start in production |

### Recommended improvements

| # | Task | Notes |
|---|---|---|
| 5 | Add **GitHub Actions CI/CD** pipeline | Automate build → test → deploy on push to main |
| 6 | **Newsletter subscription** — wire the subscribe buttons to an email list | Currently the inputs are UI-only with no backend |
| 7 | **Social links (Terms, Privacy, Refund policy)** — currently `href="#"` | Add real policy pages or links |
| 8 | **Product search performance** — add full-text index on `Products.Name` and `Products.Description` in SQL | Improves search at scale |
| 9 | **Code split the frontend** — recharts is the biggest contributor to the 982 KB bundle | Use `React.lazy()` for admin pages and recharts |
| 10 | **Admin order status email** — currently fires for all status changes; consider adding opt-out | Good for customer experience |
| 11 | **WhatsApp number** — `08033360284` appears in footer; confirm this is still the correct support number | |

---

*This document covers the state of the application as of April 2026. Keep it updated as new features are added.*
