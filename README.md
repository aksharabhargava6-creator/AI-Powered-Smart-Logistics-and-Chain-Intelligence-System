# Smart Logistics Platform — Sprint 1 (Auth + DB Schema Foundation)

This is the backend foundation for the AI-Powered Smart Logistics & Supply Chain
Intelligence Platform, covering the Sprint 1 goal: **authentication/authorization
(FR-01) + baseline DB schema for users, products, and warehouses**.

## What's included

- **ASP.NET Core 8 Web API** project structure
- **ASP.NET Core Identity** for user management, wired to SQL Server via EF Core
- **JWT bearer authentication** with role claims
- **Role-based authorization** matching Section 5 of the requirement doc:
  `SystemAdministrator`, `SupplyChainManager`, `WarehouseManager`, `FleetManager`,
  `OperationsStaff`, `Analyst` (roles auto-seeded on startup)
- **Baseline EF Core schema**: `Category`, `Product`, `Warehouse`, `InventoryBalance`
  (relationships only — stock movement logic comes in Sprint 2, FR-02/FR-03)
- **Basic CRUD Web APIs** for Categories, Products, and Warehouses (see below)
- **Swagger UI** with JWT bearer support for manual testing
- **Health check** endpoint (`/health`) per the Availability NFR
- **CORS** pre-configured for a local React dev server

## Project structure

```
LogisticsPlatform.API/
├── Controllers/
│   ├── AuthController.cs       # register / login / me
│   ├── CategoriesController.cs # CRUD for product categories
│   ├── ProductsController.cs   # CRUD for products, low-stock query
│   └── WarehousesController.cs # CRUD for warehouses, utilization query
├── Data/
│   ├── ApplicationDbContext.cs # EF Core DbContext (Identity + core entities)
│   └── RoleSeeder.cs           # seeds the 6 roles on startup
├── DTOs/
│   ├── AuthDtos.cs
│   └── CatalogDtos.cs          # Category/Product/Warehouse request & response DTOs
├── Models/
│   ├── ApplicationUser.cs      # extends IdentityUser + AppRoles constants
│   └── CoreEntities.cs         # Category, Product, Warehouse, InventoryBalance
├── Services/
│   ├── ITokenService.cs
│   └── TokenService.cs
├── Program.cs
└── appsettings.json
```

## API endpoints

All endpoints below require a `Bearer` token (from `/api/Auth/login`) except where noted.

| Method | Route | Roles allowed | Notes |
|---|---|---|---|
| POST | `/api/Auth/register` | Anyone (Admin role restricted) | |
| POST | `/api/Auth/login` | Anyone | |
| GET | `/api/Auth/me` | Any authenticated user | Inspect your own token claims |
| GET | `/api/Categories` | Any authenticated user | |
| GET | `/api/Categories/{id}` | Any authenticated user | |
| POST | `/api/Categories` | Admin, SupplyChainManager | |
| PUT | `/api/Categories/{id}` | Admin, SupplyChainManager | |
| DELETE | `/api/Categories/{id}` | Admin | Blocked if products reference it |
| GET | `/api/Products?search=&categoryId=&page=&pageSize=` | Any authenticated user | Paginated, filterable |
| GET | `/api/Products/{id}` | Any authenticated user | |
| GET | `/api/Products/low-stock` | Any authenticated user | Feeds FR-10 alerts |
| POST | `/api/Products` | Admin, SupplyChainManager, WarehouseManager | |
| PUT | `/api/Products/{id}` | Admin, SupplyChainManager, WarehouseManager | |
| DELETE | `/api/Products/{id}` | Admin, SupplyChainManager | Soft delete (IsActive = false) |
| GET | `/api/Warehouses?includeInactive=` | Any authenticated user | |
| GET | `/api/Warehouses/{id}` | Any authenticated user | |
| GET | `/api/Warehouses/{id}/utilization` | Any authenticated user | Stock vs. capacity |
| POST | `/api/Warehouses` | Admin, SupplyChainManager | |
| PUT | `/api/Warehouses/{id}` | Admin, SupplyChainManager, WarehouseManager | |
| DELETE | `/api/Warehouses/{id}` | Admin | Soft delete; blocked if stock present |

## Prerequisites

- .NET 8 SDK
- SQL Server (local instance or Docker container)
- `dotnet-ef` CLI tool: `dotnet tool install --global dotnet-ef`

## Setup

1. **Update configuration** in `appsettings.json`:
   - `ConnectionStrings:DefaultConnection` — point to your SQL Server instance
   - `JwtSettings:Secret` — replace with a real random 32+ character secret
     (never commit real secrets — use environment variables or `dotnet user-secrets`
     in practice)

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Create the initial migration** (schema doesn't exist yet — this generates it)
   ```bash
   dotnet ef migrations add InitialCreate
   ```

4. **Run the API** (applies migrations + seeds roles automatically on startup)
   ```bash
   dotnet run
   ```

5. Open Swagger at `https://localhost:<port>/swagger` to test endpoints.

## Trying it out

**Register a user:**
```
POST /api/Auth/register
{
  "fullName": "Asha Patel",
  "email": "asha@example.com",
  "password": "Passw0rd!",
  "role": "WarehouseManager"
}
```

**Log in:**
```
POST /api/Auth/login
{
  "email": "asha@example.com",
  "password": "Passw0rd!"
}
```

Copy the returned `token` into Swagger's Authorize button (`Bearer <token>`) to call
protected endpoints, e.g. `GET /api/Auth/me`.

## Notes / next steps

- Self-registration for `SystemAdministrator` is blocked in code — that role should
  be created by an existing admin or seeded manually for the first account.
- This DB schema is intentionally minimal. Sprint 2 adds stock movement history and
  transfers (FR-02), Sprint 3 adds orders/fleet, etc., per the sprint plan.
- For production, move `JwtSettings:Secret` and the DB password out of
  `appsettings.json` and into environment variables / a secrets manager.
