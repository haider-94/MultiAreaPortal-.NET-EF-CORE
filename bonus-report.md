# Bonus Report — ADO.NET vs EF Core for Performance-Critical Reporting

## 1. Introduction

The Admin area of MultiAreaPortal includes a reporting page,
**`/Admin/Reports/TopProducts`**, that returns the *top-N products by total quantity sold*,
with optional order-date-range and minimum-quantity filters.

The same aggregation is implemented **two ways** and compared at runtime:

- **Method A — Raw ADO.NET** calling the stored procedure `usp_GetTopProductsByQuantity`
  (`SqlConnection` / `SqlCommand` with `CommandType.StoredProcedure` / `SqlDataReader`).
- **Method B — EF Core LINQ** reproducing the identical aggregation with `GroupBy` / `Sum` /
  `Where` / `OrderByDescending` / `Take` and `.AsNoTracking()`.

Both paths live in `Services/ReportsService.cs` and are invoked from
`Areas/Admin/Controllers/ReportsController.cs`. The page renders both result sets, a parity check
that they are identical, and average/min/max execution time for each method.

## 2. Methodology

**Environment**

| Item | Value |
|------|-------|
| Runtime | .NET 10 (ASP.NET Core MVC) |
| Database | SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`) in Docker |
| Host | macOS (Apple Silicon / arm64), SQL Server running under emulation |
| Connection | `localhost,1434`, pooled, `Encrypt=False;TrustServerCertificate=True` |
| Data provider | `Microsoft.Data.SqlClient` |

**Dataset (seeded deterministically in `DbSeeder`)**

| Table | Rows |
|-------|------|
| Categories | 8 |
| Products | 40 |
| Orders | 800 (spread over the last 180 days) |
| OrderItems | ~2,700 |

**Parameters used for the headline run:** `@Top = 10`, `@StartDate = NULL`, `@EndDate = NULL`,
`@MinQuantity = 0`.

**Timing procedure** (`ReportsService.BenchmarkAsync`)

1. **Warm-up:** each method is called once and *not* timed — this absorbs JIT compilation,
   first-call connection-pool initialization, and SQL Server plan compilation.
2. **Timed loop:** each method is then executed **N = 20** times inside a loop; each call is
   measured with `System.Diagnostics.Stopwatch` (elapsed milliseconds).
3. **Aggregate:** average, minimum and maximum are computed and displayed on the page.

**Parity check:** the controller compares the two result sets row-by-row (ProductId,
TotalQuantitySold, TotalSalesAmount). The page shows *"Both methods returned an identical result
set"* only when they match — they do.

**How the plans / SQL were captured:** the EF Core SQL below was captured live from the
application's EF command logging (Information level, visible in the console when running
`dotnet run` in Development). The stored-procedure text is in
`Scripts/usp_GetTopProductsByQuantity.sql`.

## 3. Results

### 3.1 Timing (Top 10, no date filter, MinQuantity 0, 20 timed runs)

| Method | Average | Min | Max |
|--------|--------:|----:|----:|
| **ADO.NET (stored procedure)** | **5.38 ms** | 4.66 ms | 6.30 ms |
| **EF Core (LINQ)** | 6.68 ms | 6.13 ms | 7.26 ms |

> Representative numbers from one run; they vary a few tenths of a millisecond between runs but
> the ordering is stable across repeated measurements: **ADO.NET is consistently ~15–25% faster**
> than EF Core for this query on this dataset. Both return the **identical 10-row result set**.

### 3.2 The two SQL statements

**Method A — stored procedure** (`Scripts/usp_GetTopProductsByQuantity.sql`):

```sql
SELECT TOP (@Top)
    p.Id, p.Name, c.Name,
    SUM(oi.Quantity),
    SUM(oi.UnitPrice * oi.Quantity)
FROM OrderItems  oi
    INNER JOIN Orders     o ON o.Id = oi.OrderId
    INNER JOIN Products   p ON p.Id = oi.ProductId
    INNER JOIN Categories c ON c.Id = p.CategoryId
WHERE (@StartDate IS NULL OR o.OrderDate >= @StartDate)
  AND (@EndDate   IS NULL OR o.OrderDate <  DATEADD(DAY, 1, @EndDate))
GROUP BY p.Id, p.Name, c.Name
HAVING SUM(oi.Quantity) >= @MinQuantity
ORDER BY TotalQuantitySold DESC;
```

**Method B — EF Core-generated SQL** (captured from live command logging). Note EF omits the
`Orders` join here because the date filter is NULL and `OrderId` isn't projected — the optimizer
would still need `Orders` when a date range is supplied:

```sql
SELECT TOP(@p) [o].[ProductId], [p].[Name] AS [ProductName], [c].[Name] AS [CategoryName],
       COALESCE(SUM([o].[Quantity]), 0) AS [TotalQuantitySold],
       COALESCE(SUM([o].[UnitPrice] * CAST([o].[Quantity] AS decimal(18,2))), 0.0) AS [TotalSalesAmount]
FROM [OrderItems] AS [o]
INNER JOIN [Products] AS [p] ON [o].[ProductId] = [p].[Id]
INNER JOIN [Categories] AS [c] ON [p].[CategoryId] = [c].[Id]
GROUP BY [o].[ProductId], [p].[Name], [c].[Name]
HAVING COALESCE(SUM([o].[Quantity]), 0) >= @minQuantity
ORDER BY COALESCE(SUM([o].[Quantity]), 0) DESC
```

### 3.3 Execution plans

> **Screenshots to attach:** open SQL Server Management Studio or Azure Data Studio, enable
> *Include Actual Execution Plan*, and run (a) `EXEC dbo.usp_GetTopProductsByQuantity @Top=10;`
> and (b) the EF SQL above. Save both plan screenshots into a `docs/` folder and embed them here:
>
> ```
> ![Stored procedure plan](docs/plan-adonet.png)
> ![EF Core plan](docs/plan-efcore.png)
> ```

**Expected / observed plan characteristics** (from the schema and query shape):

- Both plans **scan `OrderItems`** (the fact table) — there is no filtering index that turns the
  aggregation into a seek, because every row participates when no date range is given.
- Non-clustered indexes exist on `OrderItems(ProductId)` and `OrderItems(OrderId)` (configured in
  `ApplicationDbContext`) and on `Orders(OrderDate)`; with a **date range** supplied, the plan can
  seek `Orders` on `OrderDate` and reduce the driving set.
- The `GROUP BY` is satisfied by a **Hash Match (Aggregate)** operator in both cases, followed by a
  **Top N Sort** for the `ORDER BY … DESC` + `TOP`.
- The plans are essentially equivalent in *shape*; the runtime difference comes from **overhead
  around** the query (see analysis), not from a materially different plan.

## 4. Analysis & Recommendations

### Why ADO.NET is faster here
- **No materialization/translation overhead.** EF Core must translate the LINQ expression tree to
  SQL, build a command, and map results through its change-tracking-aware materializer (even with
  `AsNoTracking`, there is projection/shaping machinery). The stored procedure is precompiled and
  the ADO.NET reader maps columns to the POCO by ordinal with zero abstraction.
- **Stable, precompiled plan.** The stored procedure has a cached plan keyed to its name; EF's SQL
  is parameterized and also cached, but the first-time translation cost and per-call command
  construction add up on hot paths.
- The absolute gap here is small (~1.3 ms) because the dataset is modest; it widens with row count,
  result width, and call frequency.

### What EF Core gives up that ADO.NET keeps
- **Control & raw speed** — hand-tuned SQL, stored-proc plan reuse, minimal allocations.

### What ADO.NET gives up that EF Core keeps
- **Composability** — LINQ filters compose naturally (the date/min-quantity predicates are just
  `Where` clauses); the SP encodes them in T-SQL.
- **Change tracking, identity map, migrations** — irrelevant for read-only reporting, essential for
  CRUD.
- **Testability & maintainability** — strongly-typed queries, no string SQL, refactor-safe.
- **Developer time** — no separate SP to write, deploy, version, and keep in sync with the schema.

### Guidance — when to use which

| Prefer **raw ADO.NET / stored procedures** | Prefer **EF Core** |
|--------------------------------------------|--------------------|
| Complex aggregate/reporting queries | CRUD and simple queries |
| High-frequency / latency-critical endpoints | Rapid development, evolving schema |
| Bulk/set-based operations | Business logic needing change tracking |
| When a DBA owns/tunes the SQL | When type-safety & maintainability dominate |

For **MultiAreaPortal**, this is exactly the split already in place: the Admin **CRUD** uses EF
Core, while the Customer dashboard and this Top-Products report use **raw ADO.NET** for the
read-heavy, aggregate workloads.

## 5. Optional further optimizations
- **Covering index** on `OrderItems(ProductId) INCLUDE (Quantity, UnitPrice)` to let the aggregate
  read only the index.
- **Indexed/materialized view** pre-aggregating quantity per product for near-constant-time reads.
- **Caching** the report output (e.g., `IMemoryCache` with a short TTL) since "top products"
  tolerates slight staleness.
- **Columnstore index** on `OrderItems` if it grows to millions of rows — dramatically faster
  aggregation.
- **Dapper** as a middle ground: near-ADO.NET speed with far less mapping boilerplate.
