# 🏠 HomeBroker API

A clean-architecture .NET 10 REST API for managing property listings with tiered commission calculation, JWT authentication, role-based authorization, and Cloudinary image uploads.

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Seed Data & Default Credentials](#seed-data--default-credentials)
- [API Endpoints](#api-endpoints)
- [Commission Tiers](#commission-tiers)
- [Running Tests](#running-tests)
- [Project Structure](#project-structure)
- [Troubleshooting](#troubleshooting)

---

## Tech Stack

- **.NET 10** — Web API + xUnit tests
- **Entity Framework Core 10** + SQL Server (LocalDB for dev)
- **ASP.NET Core Identity** — user/role management
- **JWT Bearer** — stateless authentication
- **Cloudinary** — property image uploads
- **IMemoryCache** — commission config & listing caching (10-min TTL)
- **Moq + xUnit** — unit testing

---

## Architecture

The solution follows **Clean Architecture** with four projects:

```
HomeBroker.Domain          → Entities, enums, repository interfaces
HomeBroker.Application     → DTOs, service interfaces, exceptions, IUnitOfWork
HomeBroker.Infrastructure  → EF DbContext, repositories, service implementations, Identity
HomeBroker.WebApi          → Controllers, middleware, DI registration, program entry
HomeBroker.Tests           → Unit tests (Services + Controllers)
```

Dependencies flow inward: `WebApi → Infrastructure → Application → Domain`.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (ships with Visual Studio) **or** any SQL Server instance
- (Optional) A [Cloudinary](https://cloudinary.com/) account for image uploads

### 1. Clone & Configure

```bash
git clone <repo-url>
cd HomeBroker
```

Open `HomeBroker.WebApi/appsettings.json` and verify/update:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HomeBroker;Trusted_Connection=true;"
  },
  "JwtConfiguration": {
    "JwtSecurityKey": "sou6+XSH83SbyGLWJ1H2RRTOgJndLrAFi79hudxN9Ew=",
    "Audience": "IES",
    "Issuer": "IES"
  },
  "CloudinarySettings": {
    "CloudName": "YOUR_CLOUD_NAME",
    "ApiKey": "YOUR_API_KEY",
    "ApiSecret": "YOUR_API_SECRET"
  }
}
```

> **Tip:** If you don't need image uploads during testing, any valid Cloudinary credentials will do — the upload is only triggered when `PropertyImage` is included in a create-listing request.

### 2. Apply Migrations

```bash
dotnet ef database update --project HomeBroker.Infrastructure --startup-project HomeBroker.WebApi
```

This creates the database and seeds default users, roles, and commission tiers.

### 3. Run the API

```bash
dotnet run --project HomeBroker.WebApi
```

Swagger UI is available at: `https://localhost:{port}/swagger`

---

## Seed Data & Default Credentials

Three users are seeded automatically on first migration. All share the same password:

| Role        | Email                    | Password    |
|-------------|--------------------------|-------------|
| Broker      | manish@broker.com        | Admin#123@  |
| HouseSeeker | rohan@houseseeker.com    | Admin#123@  |
| **Admin**   | **user@admin.com**       | Admin#123@  |

> **Note:** The Admin user exists in `SeedData.cs` but is only wired to `IdentityUserRoles` — not included in `HasData` for `ApplicationUser` in the current migration snapshot. If the admin login fails after migration, add the admin user to the `HasData` call in `SeedData.SeedUsers()` and create a new migration.

### Seeded Commission Tiers

| Tier | Price Range              | Commission |
|------|--------------------------|------------|
| 1    | ₹0 – ₹49,99,999         | 2.00%      |
| 2    | ₹50,00,000 – ₹1 crore   | 1.75%      |
| 3    | Above ₹1 crore           | 1.50%      |

---

## API Endpoints

### Authentication

All protected endpoints require a `Bearer` token in the `Authorization` header.

| Method | Endpoint              | Auth          | Description                          |
|--------|-----------------------|---------------|--------------------------------------|
| POST   | `/api/auth/login`     | None          | Login, returns JWT token             |
| POST   | `/api/auth/register`  | Admin only    | Register a new Broker or HouseSeeker |

**Login example:**
```json
POST /api/auth/login
{
  "email": "manish@broker.com",
  "password": "Admin#123@"
}
```

**Register example (Admin token required):**
```json
POST /api/auth/register
{
  "email": "new@broker.com",
  "password": "Secure#123",
  "fullName": "New Broker",
  "phoneNumber": "+977-9800000000",
  "role": "Broker"
}
```
> `role` must be `"Broker"` or `"HouseSeeker"`. Admins cannot be registered via this endpoint.

---

### Property Listings

| Method | Endpoint                                      | Auth          | Description                                  |
|--------|-----------------------------------------------|---------------|----------------------------------------------|
| POST   | `/api/propertylistings`                       | Broker        | Create listing (commission auto-calculated)  |
| GET    | `/api/propertylistings/{id}`                  | None          | Get listing by ID                            |
| GET    | `/api/propertylistings/search`                | Any auth      | Search with filters & pagination             |
| PUT    | `/api/propertylistings/{id}`                  | Broker (owner)| Update listing                               |
| DELETE | `/api/propertylistings/{id}`                  | Broker (owner)| Soft-delete listing                          |

**Search query params:** `location`, `minPrice`, `maxPrice`, `propertyType` (1–5), `pageNumber`, `pageSize`

**Property types:** `1=Apartment`, `2=House`, `3=Villa`, `4=Land`, `5=Commercial`

> Commission (`EstimatedCommission`) is returned as `0` for all users except the listing's own broker.

---

### Commission Configuration

| Method | Endpoint                             | Auth  | Description                     |
|--------|--------------------------------------|-------|---------------------------------|
| GET    | `/api/commissionconfiguration`       | Admin | Get all commission tiers        |
| GET    | `/api/commissionconfiguration/{id}`  | Admin | Get tier by ID                  |
| POST   | `/api/commissionconfiguration`       | Admin | Create a new tier               |
| PUT    | `/api/commissionconfiguration`       | Admin | Update an existing tier         |

**Also available on the listings controller (Broker auth):**

| Method | Endpoint                                          | Description                        |
|--------|---------------------------------------------------|------------------------------------|
| GET    | `/api/propertylistings/commissions/configurations`| View all active tiers              |
| GET    | `/api/propertylistings/commissions/calculate?price=5000000` | Preview commission for a price |

---

## Commission Tiers

Commission is calculated at listing creation and recalculated on price update. Tiers are cached for **10 minutes**; changes via the API take effect within one cache TTL without restarting the app.

**Calculation formula:** `commission = round((price × percentage) / 100, 2)`

Overlapping tier ranges are rejected by the service with a validation error.

---

## Running Tests

```bash
# All tests
dotnet test HomeBroker.Tests

# Specific class
dotnet test HomeBroker.Tests --filter "ClassName=CommissionServiceTests"
dotnet test HomeBroker.Tests --filter "ClassName=PropertyListingServiceTests"
dotnet test HomeBroker.Tests --filter "ClassName=PropertyListingsControllerTests"

# Verbose output
dotnet test HomeBroker.Tests -v detailed
```

### Test Coverage Summary

**CommissionServiceTests** — 8 tests
- Commission calculation across all tiers
- Zero price → zero commission
- Very high price with no `MaxPrice` bound
- Empty config → `InvalidOperationException`
- Cache hit on repeated calls
- Theory-based multi-tier assertions

**PropertyListingServiceTests** — 4 tests
- `GetListingByIdAsync` with invalid ID → `BadRequestException`
- `UpdateListingAsync` with mismatched broker → `UnauthorizedAccessException`
- Cache hit on second `GetListingByIdAsync` call
- `SearchListingsAsync` with filters and broker hydration

**PropertyListingsControllerTests** — 12 tests
- GET, POST, PUT, DELETE happy paths
- Parameter forwarding verification
- Broker ID extracted from `IUserMeta`
- Empty search results
- Filter passthrough to service

> Controller tests use `Moq` and do **not** require a database or running API.

---

## Project Structure

```
HomeBroker.Domain/
├── Entities/
│   ├── PropertyListing.cs
│   └── CommissionConfiguration.cs
├── Enums/PropertyType.cs
└── IRepositories/

HomeBroker.Application/
├── DTOs/
├── Exceptions/
├── IServiceInterfaces/
│   ├── ICommissionService/
│   ├── IPropertyListingService/
│   └── IUserMeta/
└── IUnitOfWork.cs

HomeBroker.Infrastructure/
├── DbContext/HomeBrokerDbContext.cs
├── Identity/ApplicationUser.cs
├── Migrations/
├── Repositories/
├── Services/
│   ├── AuthService.cs
│   ├── JwtTokenService.cs
│   ├── CommissionService/
│   ├── ImageService/
│   ├── PropertyListingService/
│   └── UserMeta/
├── SeedData.cs
└── UnitOfWork.cs

HomeBroker.WebApi/
├── Controllers/
├── Middleware/ExceptionHandlingMiddleware.cs
├── Authorization/RoleRequirement.cs
├── RegisterDependencies.cs
└── Program.cs

HomeBroker.Tests/
├── Controllers/PropertyListingsControllerTests.cs
└── Services/
    ├── CommissionServiceTests.cs
    └── PropertyListingServiceTests.cs
```

---

## Troubleshooting

**Migration fails / DB not found**
Ensure LocalDB is installed and the connection string matches your environment. For a full SQL Server instance, update `DefaultConnection` in `appsettings.json`.

**Admin user can't log in**
The `admin` variable in `SeedData.SeedUsers()` is defined but not passed to `HasData`. Add `admin` to the `builder.Entity<ApplicationUser>().HasData(broker, houseSeeker, admin)` call, then run `dotnet ef migrations add AddAdminUser` and `dotnet ef database update`.

**Commission not calculated / `InvalidOperationException`**
The `CommissionConfigurations` table is empty. Re-run migrations or manually insert at least one tier via `POST /api/commissionconfiguration` with an Admin token.

**Image upload fails**
Verify Cloudinary credentials in `appsettings.json`. Images must be JPEG, PNG, or WebP and under 5 MB. If testing locally without Cloudinary, omit the `PropertyImage` field — a listing can be created with an empty `ImageUrl`.

**`UserMeta` throws on unauthenticated requests**
`GetListing` (by ID) calls `_meta.GetUserId()` even for anonymous requests. Ensure an `Authorization: Bearer <token>` header is present, or expect a 500 from the middleware if the token is missing.

**Tests fail with EF-related errors**
Tests use in-memory mocks via `Moq` and do not touch a real database. If you see EF errors in tests, check that `DbContextOptions` are not accidentally being resolved from DI in a test constructor.

---

## Notes for Testers

- Use Swagger UI (`/swagger`) to interactively test all endpoints — the Bearer token box is pre-configured.
- Paste the JWT token from `/api/auth/login` into the Swagger "Authorize" dialog.
- The `SearchListings` endpoint requires any valid JWT (any role); `CreateListing`, `UpdateListing`, and `DeleteListing` require a **Broker** token.
- Commission configuration endpoints require an **Admin** token.
- Soft-deleted listings are excluded from search results but are still present in the database.
- Cache TTL is 5 minutes for listings and 10 minutes for commission configs. If you update a tier and commission doesn't reflect immediately, wait for the TTL or restart the API.