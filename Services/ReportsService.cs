using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MultiAreaPortal.Areas.Admin.Models;
using MultiAreaPortal.Data;
using MultiAreaPortal.Models;

namespace MultiAreaPortal.Services;

// Retrieves the "top products by quantity" report two ways — raw ADO.NET via a stored
// procedure, and EF Core LINQ — so their performance can be compared side by side.
public class ReportsService(ApplicationDbContext db, IConfiguration configuration)
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    // ---- Method A: raw ADO.NET calling the stored procedure --------------------------------
    public async Task<List<TopProductViewModel>> GetTopProductsAdoNetAsync(
        int top, DateTime? startDate, DateTime? endDate, int minQuantity, CancellationToken ct = default)
    {
        var results = new List<TopProductViewModel>();

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("dbo.usp_GetTopProductsByQuantity", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@Top", SqlDbType.Int) { Value = top });
        command.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date)
        { Value = (object?)startDate ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.Date)
        { Value = (object?)endDate ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@MinQuantity", SqlDbType.Int) { Value = minQuantity });

        await connection.OpenAsync(ct);
        using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new TopProductViewModel
            {
                ProductId = reader.GetInt32(0),
                ProductName = reader.GetString(1),
                CategoryName = reader.GetString(2),
                TotalQuantitySold = reader.GetInt32(3),
                TotalSalesAmount = reader.GetDecimal(4)
            });
        }

        return results;
    }

    // ---- Method B: EF Core LINQ reproducing the same aggregation ---------------------------
    public async Task<List<TopProductViewModel>> GetTopProductsEfCoreAsync(
        int top, DateTime? startDate, DateTime? endDate, int minQuantity, CancellationToken ct = default)
    {
        // Match the stored procedure's inclusive end-of-day semantics.
        var endExclusive = endDate?.Date.AddDays(1);

        var query = db.OrderItems.AsNoTracking()
            .Where(oi => (startDate == null || oi.Order!.OrderDate >= startDate)
                      && (endExclusive == null || oi.Order!.OrderDate < endExclusive))
            .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, CategoryName = oi.Product!.Category!.Name })
            .Select(g => new TopProductViewModel
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                CategoryName = g.Key.CategoryName,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalSalesAmount = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .Where(r => r.TotalQuantitySold >= minQuantity)
            .OrderByDescending(r => r.TotalQuantitySold)
            .Take(top);

        return await query.ToListAsync(ct);
    }

    // ---- Benchmark: warm-up once, then time N runs -----------------------------------------
    public async Task<BenchmarkResult> BenchmarkAsync(
        string method,
        Func<CancellationToken, Task<List<TopProductViewModel>>> call,
        int runs,
        CancellationToken ct = default)
    {
        await call(ct); // warm-up (JIT, plan cache, connection pool) — not timed

        var samples = new double[runs];
        for (var i = 0; i < runs; i++)
        {
            var sw = Stopwatch.StartNew();
            await call(ct);
            sw.Stop();
            samples[i] = sw.Elapsed.TotalMilliseconds;
        }

        return new BenchmarkResult(method, runs, samples.Average(), samples.Min(), samples.Max());
    }
}
