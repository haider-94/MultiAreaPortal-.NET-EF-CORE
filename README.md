# MultiAreaPortal — Event Management Portal

A role-based **ASP.NET Core MVC** (.NET 10) portal with three areas, **ASP.NET Core Identity**
(Individual Accounts), and **hybrid data access**: **Entity Framework Core** for the Admin event
CRUD and **raw ADO.NET** for the Customer reporting dashboard. Data is stored in **SQL Server 2022**
running in Docker.

## Areas & roles

| Area       | Access                | Data access        | Purpose |
|------------|-----------------------|--------------------|---------|
| `Public`   | Anonymous             | none               | Welcome/landing page. Open to everyone. |
| `Customer` | `Customer` role       | **Raw ADO.NET**    | Event dashboard: total event count + 5 most recent events. |
| `Admin`    | `Admin` role          | **EF Core** (CRUD) | Manage events (create / edit / delete / list). |
| `Identity` | (framework)           | EF Core            | Login / Register / Logout / Manage (ASP.NET Core Identity UI — Razor Pages). |

Authorization is enforced with `[Authorize(Roles = "...")]` at the area-controller level:
- Unauthenticated users hitting Admin or Customer are redirected to `/Identity/Account/Login`.
- An **Admin** can reach `/Admin/Events` but **not** `/Customer/Dashboard` (not in Customer role).
- A **Customer** can reach `/Customer/Dashboard` but **not** `/Admin/Events`.

## How it was scaffolded

```bash
dotnet new mvc -au Individual -n MultiAreaPortal
```

This template ships ASP.NET Core Identity, `ApplicationDbContext`, and the Razor Pages login/register
UI. It was then converted from the template's default **SQLite** provider to **SQL Server**, and the
three MVC areas were added on top. The prebuilt Identity screens live in `Areas/Identity/Pages/…`
(Razor Pages) and are the only non-MVC part of the app; all portal features are classic MVC.

## Project structure

```
MultiAreaPortal/
├─ docker-compose.yml                 # SQL Server 2022 container (host port 1434)
├─ Program.cs                         # EF Core (SqlServer), Identity + roles, area route, MapRazorPages, seeding
├─ appsettings.json                   # Connection string + seed credentials
├─ Data/
│  ├─ ApplicationDbContext.cs         # IdentityDbContext + DbSet<Event>
│  ├─ DbSeeder.cs                     # Migrates + seeds Admin/Customer roles and one test user each
│  └─ Migrations/                     # InitialCreate (Identity + Events)
├─ Models/Event.cs                    # Event entity (Name, Description, TicketPrice, EventDate, CreatedAt)
├─ Controllers/HomeController.cs      # Root "/" → redirects to Public area
├─ Areas/
│  ├─ Admin/    Controllers/EventsController.cs      + Views/Events/*  (EF Core CRUD)
│  ├─ Customer/ Controllers/DashboardController.cs   + Views/Dashboard/Index.cshtml  (RAW ADO.NET)
│  │            Models/DashboardViewModel.cs
│  ├─ Public/   Controllers/HomeController.cs        + Views/Home/Index.cshtml  (anonymous)
│  └─ Identity/ Pages/Account/Register.cshtml(.cs)   # override: new sign-ups get the Customer role
└─ Views/Shared/_Layout.cshtml        # role-aware navigation + _LoginPartial
```

## Hybrid data access

- **EF Core** (`Microsoft.EntityFrameworkCore.SqlServer`) — Admin `EventsController` uses
  `DbSet<Event>` + LINQ + `SaveChangesAsync` for all CRUD, and it is the Identity store.
- **Raw ADO.NET** (`Microsoft.Data.SqlClient`) — Customer `DashboardController` injects only
  `IConfiguration`, opens a `SqlConnection`, and runs two parameter-free `SqlCommand`s
  (`SELECT COUNT(*) FROM Events` and `SELECT TOP 5 … ORDER BY CreatedAt DESC`), mapping the
  reader to a view model. **No EF Core is used in that controller.**

## Database (Docker)

A dedicated SQL Server 2022 instance on **host port 1434** (isolated from any other local instance):

```bash
docker compose up -d          # container "multiareaportal-sql"
```

Connection string (`appsettings.json`):

```
Server=localhost,1434;Database=MultiAreaPortalDb;User Id=sa;Password=Portal@Pass2022;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True
```

The schema (EF migration) and role/user seed data are applied automatically on startup by
`DbSeeder.SeedAsync`.

## Run

```bash
docker compose up -d
dotnet run
# http://localhost:5216   (https://localhost:7150)
```

## Seeded test accounts

| Role     | Email                 | Password       |
|----------|-----------------------|----------------|
| Admin    | admin@portal.com      | `Admin@123`    |
| Customer | customer@portal.com   | `Customer@123` |

New self-service registrations (`/Identity/Account/Register`) are automatically placed in the
**Customer** role.

## EF Core migrations

```bash
dotnet ef migrations add <Name>
dotnet ef database update        # also applied automatically on startup
```

## Bonus: Top Products performance report (ADO.NET vs EF Core)

An Admin-only reporting page compares raw ADO.NET (stored procedure) against EF Core LINQ for the
same aggregation, with live timing. See **`bonus-report.md`** for the full analysis.

- **Stored procedure:** `Scripts/usp_GetTopProductsByQuantity.sql` — installed automatically on
  startup (`CREATE OR ALTER`, idempotent) by `DbSeeder`.
- **Sales data** (Categories/Products/Orders/OrderItems) is seeded automatically on first run.
- **Service:** `Services/ReportsService.cs` — `GetTopProductsAdoNetAsync` (SqlConnection/SqlCommand/
  SqlDataReader + stored proc) and `GetTopProductsEfCoreAsync` (LINQ `GroupBy`/`Sum`/`Take`,
  `AsNoTracking`), plus `BenchmarkAsync` (warm-up + timed loop).
- **Page:** `/Admin/Reports/TopProducts` (nav: **Reports**, Admin only).

**To run it:**
1. `docker compose up -d` && `dotnet run`, then log in as **admin@portal.com / Admin@123**.
2. Go to **Reports** in the navbar (or `/Admin/Reports/TopProducts`).
3. Set *Top*, optional *Start/End date*, *Min quantity*, *Benchmark runs*, then **Run & Compare**.
4. The page shows both result sets (with a parity check that they're identical) and the average /
   min / max execution time for each method.

To capture execution plans for the report, run `EXEC dbo.usp_GetTopProductsByQuantity @Top=10;` and
the EF-generated SQL (in `bonus-report.md`) in SSMS/Azure Data Studio with *Include Actual Execution
Plan* enabled.

## Manual test walkthrough

1. `docker compose up -d` then `dotnet run`.
2. Visit `/` → Public landing (no login).
3. Log in as **Admin** → **Admin Panel** → create / edit / delete events.
4. Log in as **Customer** → **Customer Dashboard** → see the event count + 5 most recent events.
5. As Customer, browse to `/Admin/Events` → Access Denied. Log out, hit `/Admin/Events` → redirected to login.
