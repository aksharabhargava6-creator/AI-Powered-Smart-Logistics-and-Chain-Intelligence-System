# AI-Powered Smart Logistics & Supply Chain Intelligence System

An ASP.NET Core 8 platform for managing warehouses, inventory, fleet, drivers, live delivery
tracking, route optimization, alerts, and audit logging — with an optional Python/FastAPI
microservice for demand forecasting and ETA prediction. Built as a team capstone project,
with each functional area owned by a different member .

## Table of contents

- [Overview](#overview)
- [Tech stack](#tech-stack)
- [Project structure](#project-structure)
- [Feature modules](#feature-modules)
- [Roles & authorization](#roles--authorization)
- [API surface](#api-surface)
- [MVC pages](#mvc-pages)
- [Predictive Analytics microservice (Python)](#predictive-analytics-microservice-python)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [Known issues / integration gaps](#known-issues--integration-gaps)
- [Roadmap](#roadmap)

## Overview

The system is organized around a supply-chain lifecycle:

1. **Catalog & inventory** — categories, products, warehouses, stock balances
2. **Fleet & drivers** — vehicle and driver records, status, assignment
3. **Live tracking** — GPS location updates pushed over SignalR, with a simulator for demos
4. **Route optimization** — nearest-neighbor route planner with distance/time/fuel-cost estimates
5. **AI Assistant** — rule-based Q&A over route and operations data
6. **Alerts** — low-stock, delayed-delivery, and other operational alerts
7. **Audit logging** — every controller action is recorded automatically
8. **Predictive analytics** — a separate FastAPI service for demand forecasting and ETA prediction

The web project exposes both **JSON APIs** (`/api/...`, consumed by a SPA/mobile client or
Swagger) and **server-rendered MVC pages** (Razor views under `Views/`) for a couple of the
modules (dashboard, AI Assistant, Account, Warehouses, Vehicles, Drivers, Route Optimization,
Audit Logs, Product).

## Tech stack

| Layer | Technology |
|---|---|
| Backend framework | ASP.NET Core 8 (Web API + MVC in one project) |
| Auth | ASP.NET Core Identity + JWT bearer (API) and cookie auth (MVC views) |
| ORM | Entity Framework Core 8, SQL Server |
| Real-time | SignalR (`/trackingHub`) |
| API docs | Swashbuckle / Swagger UI |
| Predictive analytics | Python 3, FastAPI, pandas, scikit-learn (separate microservice) |
| Frontend (MVC) | Razor views (`.cshtml`) |

## Project structure

```
AI-Powered-Smart-Logistics-and-Chain-Intelligence-System-main/
├── Program.cs                       # ACTIVE application entry point
├── fleet_Program.cs                 # alternate entry point (Fleet/Tracking module) — see Known Issues
├── Smart_Route_Program.cs           # alternate entry point (Route/AI module) — see Known Issues
├── appsettings.json
├── LogisticsPlatform.API.csproj
│
├── Controllers/
│   ├── AuthController.cs            # JWT register/login/me
│   ├── AccountController.cs         # cookie-based login/register for MVC views
│   ├── CategoriesController.cs      # catalog CRUD
│   ├── ProductsController.cs        # product CRUD + low-stock query
│   ├── WarehousesController.cs      # warehouse CRUD + utilization query
│   ├── VehiclesController.cs        # fleet CRUD, status, live location, history
│   ├── DriversController.cs         # driver CRUD, availability, status
│   ├── RouteOptimizationController.cs  # route optimization + distance calc
│   ├── AIAssistantController.cs     # natural-language query endpoint
│   ├── Member7AlertsController.cs   # operational alerts API
│   ├── Member7DashboardController.cs
│   ├── AuditLogControllers.cs       # audit log viewer (MVC)
│   └── HomeController.cs
│
├── Data/
│   ├── ApplicationDbContext.cs      # Identity + Category/Product/Warehouse/InventoryBalance
│   ├── SmartLogisticsContext.cs     # User/Product/Warehouse/Inventory/Order/Delivery/Forecast/AuditLog
│   ├── LogisticsDbContext.cs        # Vehicle/Driver/VehicleLocation (fleet tracking)
│   └── RoleSeeder.cs                # seeds the 6 roles on startup
│
├── Models/
│   ├── ApplicationUser.cs           # Identity user + AppRoles constants
│   ├── LogisticsModels.cs           # User, Product, Warehouse, Inventory, Order, DeliveryAssignment, DemandForecast
│   ├── AuditLog.cs
│   ├── AIQueryRequest.cs / AIQueryResponse.cs
│   ├── RouteRequest.cs / RouteResponse.cs / DeliveryPoint.cs
│   ├── Member7DashboardModels.cs
│   └── fleet_tracking/
│       ├── Driver.cs
│       ├── Vehicle.cs
│       └── VehicleLocation.cs
│
├── DTOs/
│   ├── AuthDtos.cs, CatalogDtos.cs, DriverDto.cs, VehicleDto.cs
│   └── LocationUpdateDto.cs, VehicleLocationHistoryDto.cs
│
├── Services/
│   ├── TokenService.cs / ITokenService.cs         # JWT issuing
│   ├── VehicleService.cs / IVehicleService.cs
│   ├── DriverService.cs / IDriverService.cs
│   ├── TrackingService.cs / ITrackingService.cs   # location updates, in-memory status cache
│   ├── RouteOptimizationService.cs / IRouteOptimizationService.cs
│   ├── AIAssistantService.cs / IAIAssistantService.cs
│   ├── Member7AlertService.cs                     # low-stock / operational alerts
│   └── AiServices.cs                              # AiEngineService (misc AI helper)
│
├── Simulations/
│   └── GpsSimulatorService.cs       # background service that fakes vehicle GPS movement
│
├── Filters/
│   └── AuditLogFilters.cs           # global IAsyncActionFilter, logs every action to AuditLog
│
├── Hub/
│   └── TrackingHub.cs               # SignalR hub broadcasting ReceiveLocationUpdate
│
├── Views/                           # Razor views for Account, Home, Product, Warehouses,
│                                     # Vehicles, Drivers, RouteOptimization, AIAssistant,
│                                     # Member7Dashboard, AuditLogs, Shared/_Layout
│
└── PredictiveAnalytics/             # standalone Python/FastAPI microservice
    ├── main.py                      # FastAPI app: /api/forecast/*, /api/eta/predict, /health
    ├── forecasting.py               # demand forecasting + replenishment flag logic
    ├── eta.py                       # ETA / distance prediction logic
    └── requirements.txt
```

## Feature modules

### 1. Authentication & authorization
- ASP.NET Core Identity (`ApplicationUser : IdentityUser`) with JWT bearer tokens for the API
  and cookie auth for the Razor views (`/Account/Login`).
- Six roles, auto-seeded on startup via `RoleSeeder`:
  `SystemAdministrator`, `SupplyChainManager`, `WarehouseManager`, `FleetManager`,
  `OperationsStaff`, `Analyst`.
- Self-registration as `SystemAdministrator` is blocked in code; that role must be assigned
  by an existing admin or seeded manually.

### 2. Catalog & inventory
- `Category`, `Product`, `Warehouse`, `InventoryBalance` entities (`ApplicationDbContext`).
- A parallel, simpler `Product` / `Warehouse` / `Inventory` model set lives in
  `SmartLogisticsContext` (used by the alerts, orders, and forecast features) — see
  [Known Issues](#known-issues--integration-gaps).
- CRUD endpoints with paging/filtering on products, low-stock lookup, warehouse utilization
  (stock vs. capacity).

### 3. Fleet & driver management
- `Vehicle` (registration, type, capacity, status, assigned driver, current lat/lng) and
  `Driver` (name, phone, license number/expiry, status) entities.
- Full CRUD plus status transitions (`Available`, `OnDuty`/`InTransit`, `OffDuty`,
  `Maintenance`, `Offline`), driver-to-vehicle assignment, and available-driver/vehicle
  lookups.

### 4. Live GPS tracking
- `TrackingService` validates and records lat/lng/speed/heading updates into
  `VehicleLocation` history, keeps an in-memory `ConcurrentDictionary` cache of latest
  vehicle status, and — via `TrackingHub` (SignalR) — pushes `ReceiveLocationUpdate` events
  to connected clients in real time.
- `GpsSimulatorService` is a hosted background service that generates fake movement so the
  tracking UI/API has live data without real hardware.

### 5. Route optimization
- `RouteOptimizationService` implements a nearest-neighbor heuristic over a starting point
  and a list of delivery points, returning ordered route segments plus total distance,
  estimated time, and estimated fuel cost (`FUEL_COST_PER_KM`, average speed, average stop
  time constants).
- `OptimizeRoutesWithConstraints` additionally splits stops across multiple routes when a
  `maxStopsPerRoute` limit is supplied.
- A `distance` endpoint exposes a standalone great-circle (haversine) distance calculation.

### 6. AI Assistant
- `AIAssistantController` / `AIAssistantService` implement a lightweight, keyword-matching
  Q&A layer over operational topics (route efficiency, inventory, cost, delivery priority)
  and can pull a live route recommendation by calling `IRouteOptimizationService`.
- This is a rules-based simulation of an AI assistant, not an LLM integration.

### 7. Alerts (Member 7 — FR-10)
- `Member7AlertService.GetOperationalAlertsAsync()` scans `Inventory` (via
  `SmartLogisticsContext`) and raises **low-stock alerts** when
  `QuantityOnHand - QuantityReserved` drops below a threshold, surfaced through
  `Member7AlertsController` (`GET /api/member7/alerts`) and `Member7Dashboard`.

### 8. Audit logging (FR-13)
- `AuditLogFilter` is registered globally in `Program.cs` and fires on **every** controller
  action, recording user, HTTP method, controller/action, path, status code, IP address, and
  UTC timestamp to the `AuditLog` table (skips GET requests to the audit log viewer itself to
  avoid noise). Viewable at `/AuditLogs` (requires authentication).

### 9. Predictive analytics (Python microservice)
See [Predictive Analytics microservice](#predictive-analytics-microservice-python) below —
this is a separate FastAPI app, not part of the ASP.NET Core project, intended to be called
by the backend (FR-07 demand forecasting, FR-08 ETA prediction).

## Roles & authorization

| Role | Typical permissions in this codebase |
|---|---|
| `SystemAdministrator` | Full access; only role that can delete categories/warehouses |
| `SupplyChainManager` | Create/update categories, products, warehouses |
| `WarehouseManager` | Create/update products; update warehouses |
| `FleetManager` | Intended owner of fleet/driver management (not yet enforced via `[Authorize(Roles=...)]` on `VehiclesController`/`DriversController` — see Known Issues) |
| `OperationsStaff` | Day-to-day operational use (read access) |
| `Analyst` | Reporting / read access |

## API surface

All endpoints below are relative to the app root. Endpoints under `[Authorize]` require a
`Bearer <token>` obtained from `POST /api/Auth/login`.

### Auth
| Method | Route | Notes |
|---|---|---|
| POST | `/api/Auth/register` | Self-registration; Admin role is blocked |
| POST | `/api/Auth/login` | Returns JWT |
| GET | `/api/Auth/me` | Requires auth; inspect your own claims |

### Catalog
| Method | Route | Roles | Notes |
|---|---|---|---|
| GET | `/api/Categories`, `/api/Categories/{id}` | Any authenticated user | |
| POST / PUT | `/api/Categories[/{id}]` | Admin, SupplyChainManager | |
| DELETE | `/api/Categories/{id}` | Admin | Blocked if products reference it |
| GET | `/api/Products?search=&categoryId=&page=&pageSize=` | Any authenticated user | Paginated/filterable |
| GET | `/api/Products/{id}`, `/api/Products/low-stock` | Any authenticated user | Low-stock feeds FR-10 alerts |
| POST / PUT | `/api/Products[/{id}]` | Admin, SupplyChainManager, WarehouseManager | |
| DELETE | `/api/Products/{id}` | Admin, SupplyChainManager | Soft delete (`IsActive = false`) |
| GET | `/api/Warehouses?includeInactive=`, `/api/Warehouses/{id}` | Any authenticated user | |
| GET | `/api/Warehouses/{id}/utilization` | Any authenticated user | Stock vs. capacity |
| POST | `/api/Warehouses` | Admin, SupplyChainManager | |
| PUT | `/api/Warehouses/{id}` | Admin, SupplyChainManager, WarehouseManager | |
| DELETE | `/api/Warehouses/{id}` | Admin | Soft delete; blocked if stock present |

### Fleet & drivers
| Method | Route | Notes |
|---|---|---|
| GET | `/api/Vehicles`, `/api/Vehicles/{id}` | |
| GET | `/api/Vehicles/status/{status}` | Filter by status |
| GET | `/api/Vehicles/{id}/location`, `/api/Vehicles/{id}/location-history` | Latest + historical GPS |
| POST / PUT | `/api/Vehicles[/{id}]` | |
| PUT | `/api/Vehicles/{id}/status` | |
| PUT | `/api/Vehicles/{vehicleId}/assign-driver/{driverId}` | |
| DELETE | `/api/Vehicles/{id}` | |
| GET | `/api/Drivers`, `/api/Drivers/available`, `/api/Drivers/{id}` | |
| POST / PUT | `/api/Drivers[/{id}]` | |
| PUT | `/api/Drivers/{id}/status` | |
| DELETE | `/api/Drivers/{id}` | |

### Route optimization & AI Assistant
| Method | Route | Notes |
|---|---|---|
| POST | `/api/RouteOptimization/optimize` | Nearest-neighbor route over delivery points |
| POST | `/api/RouteOptimization/distance` | Haversine distance between two points |
| POST | `/api/RouteOptimization/optimize-with-constraints?maxStopsPerRoute=20` | Splits stops across multiple routes |
| POST | `/api/AIAssistant/query` | Natural-language operational query |
| GET | `/api/AIAssistant/route-recommendation` | AI-generated summary of an optimized route |

### Alerts
| Method | Route | Notes |
|---|---|---|
| GET | `/api/member7/alerts` | Low-stock (and other operational) alerts |

### Real-time
| Hub | Route | Event |
|---|---|---|
| `TrackingHub` | `/trackingHub` | `ReceiveLocationUpdate(deliveryId, lat, lng, speed, time)` |

### Health
| Method | Route |
|---|---|
| GET | `/health` *(referenced in original Sprint 1 design; verify current wiring in `Program.cs` before relying on it)* |

## MVC pages

Server-rendered Razor views (cookie auth) are available for:
`/Account/Login`, `/Account/Register`, `/Home`, `/Product`, `/Warehouses`, `/Vehicles`,
`/Drivers`, `/RouteOptimization`, `/AIAssistant`, `/Member7Dashboard`, `/AuditLogs`
(all under the shared `_Layout.cshtml`).

## Predictive Analytics microservice (Python)

A standalone FastAPI service under `PredictiveAnalytics/`, intended to be called by the
ASP.NET Core backend rather than run inline with it.

```bash
cd PredictiveAnalytics
python -m venv .venv
source .venv/bin/activate        # Windows: .venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --reload --port 8001
```

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/forecast/demand` | Demand forecast for a product over a horizon (days), from order history |
| POST | `/api/forecast/replenishment-flags` | Flags whether predicted demand breaches a safety-stock threshold |
| POST | `/api/eta/predict` | Predicts delivery ETA and distance between origin/destination coordinates |
| GET | `/health` | Health check |

`forecasting.py` raises `InsufficientDataError` (→ HTTP 422) when there isn't enough order
history to forecast; `eta.py` raises `InvalidCoordinateError` (→ HTTP 422) for malformed
lat/lng input.

## Getting started

### Prerequisites
- .NET 8 SDK
- SQL Server (local instance or Docker container)
- `dotnet-ef` CLI: `dotnet tool install --global dotnet-ef`
- Python 3.10+ (only if running the Predictive Analytics service)

### Setup (ASP.NET Core app)

1. **Configure** `appsettings.json`:
   - `ConnectionStrings:DefaultConnection` — point at your SQL Server instance
   - Add a `JwtSettings:Secret` (32+ random characters) if not already present — required by
     `TokenService`. Never commit real secrets; prefer environment variables or
     `dotnet user-secrets` for local development.

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Create the database migration** (first run — no schema exists yet)
   ```bash
   dotnet ef migrations add InitialCreate
   ```
   Because three separate `DbContext` classes exist (`ApplicationDbContext`,
   `SmartLogisticsContext`, `LogisticsDbContext`), you'll likely need to target each one
   explicitly, e.g. `dotnet ef migrations add InitialCreate --context SmartLogisticsContext`
   — see [Known Issues](#known-issues--integration-gaps).

4. **Run**
   ```bash
   dotnet run
   ```
   Migrations/role seeding run automatically on startup for the contexts wired into
   `Program.cs`.

5. Open `https://localhost:<port>/swagger` for the API, or `/` for the MVC dashboard.

### Trying the API

```
POST /api/Auth/register
{
  "fullName": "Asha Patel",
  "email": "asha@example.com",
  "password": "Passw0rd!",
  "role": "WarehouseManager"
}

POST /api/Auth/login
{
  "email": "asha@example.com",
  "password": "Passw0rd!"
}
```

Copy the returned `token` into Swagger's **Authorize** button as `Bearer <token>` to call
protected endpoints, e.g. `GET /api/Auth/me`.

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string, shared by all `DbContext`s |
| `JwtSettings:Secret` (and related JWT settings) | Signs/validates JWT tokens issued by `TokenService` |
| `Logging`, `AllowedHosts` | Standard ASP.NET Core settings |

For production, move secrets out of `appsettings.json` into environment variables or a
secrets manager, and set the CORS origin(s) to your actual frontend domain instead of
`AllowAnyOrigin`/`localhost:3000`.

## Known issues / integration gaps

This project was assembled from multiple members' independently-built modules, so a few
integration points still need reconciliation before a clean build/run:

- **Three duplicate top-level entry points**: `Program.cs`, `fleet_Program.cs`, and
  `Smart_Route_Program.cs` each contain top-level statements. Only one `Program.cs` per
  executable is valid in .NET — `fleet_Program.cs` and `Smart_Route_Program.cs` will need to
  be merged into `Program.cs` (registering `IVehicleService`/`IDriverService`,
  `IRouteOptimizationService`/`IAIAssistantService`, `LogisticsDbContext`, CORS policies,
  and the GPS simulator hosted service) or removed, or the build will fail.
- **Three separate `DbContext`s over overlapping domains**: `ApplicationDbContext` (Identity
  + Category/Product/Warehouse/InventoryBalance), `SmartLogisticsContext`
  (User/Product/Warehouse/Inventory/Order/DeliveryAssignment/DemandForecast/AuditLog), and
  `LogisticsDbContext` (Vehicle/Driver/VehicleLocation) model **Product** and **Warehouse**
  independently and aren't linked by foreign keys across contexts. Only
  `ApplicationDbContext` and `SmartLogisticsContext` are currently registered in
  `Program.cs` — `LogisticsDbContext` (and therefore `TrackingService`, `VehicleService`,
  `DriverService`) is not wired up for dependency injection there.
- **Role enforcement gaps**: `VehiclesController` and `DriversController` don't yet carry
  `[Authorize(Roles = ...)]` attributes restricting writes to `FleetManager`/Admin, unlike
  the catalog controllers.
- **AI Assistant is rule-based**, not backed by an LLM — treat `AIAssistantService`'s
  keyword matching as a placeholder for a real NLP/LLM integration.
- **Predictive Analytics service is not yet called from the .NET backend** — it runs
  standalone; wiring an `HttpClient` from a C# service into `PredictiveAnalytics`'s FastAPI
  endpoints is still open work.

## Roadmap

Per the original sprint plan, this Sprint 1 foundation (auth + baseline catalog schema) has
since grown to include fleet tracking, route optimization, alerts, and audit logging.
Suggested next steps:
1. Resolve the entry-point and `DbContext` fragmentation above.
2. Wire role-based authorization onto fleet/driver endpoints.
3. Connect the Predictive Analytics FastAPI service to the backend (forecast-driven
   replenishment, ETA-driven delivery tracking).
4. Replace the rule-based AI Assistant with a real model integration if desired.
5. Add automated tests around route optimization, alerting thresholds, and audit logging.
