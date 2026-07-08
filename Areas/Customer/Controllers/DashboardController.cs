using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MultiAreaPortal.Areas.Customer.Models;

namespace MultiAreaPortal.Areas.Customer.Controllers;

// Customer dashboard. Reads data using RAW ADO.NET only (SqlConnection / SqlCommand / SqlDataReader).
// No EF Core / ORM is used anywhere in this controller, per the assignment.
[Area("Customer")]
[Authorize(Roles = "Customer")]
public class DashboardController(IConfiguration configuration) : Controller
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    // GET: /Customer/Dashboard
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = new DashboardViewModel();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        // Query 1: total number of events.
        using (var countCommand = new SqlCommand("SELECT COUNT(*) FROM Events", connection))
        {
            model.TotalEvents = (int)(await countCommand.ExecuteScalarAsync(ct))!;
        }

        // Query 2: the five most recently created events.
        const string recentSql =
            "SELECT TOP 5 Id, Name, TicketPrice, EventDate, CreatedAt FROM Events ORDER BY CreatedAt DESC";
        var recent = new List<RecentEvent>();
        using (var recentCommand = new SqlCommand(recentSql, connection))
        using (var reader = await recentCommand.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                recent.Add(new RecentEvent(
                    Id: reader.GetInt32(0),
                    Name: reader.GetString(1),
                    TicketPrice: reader.GetDecimal(2),
                    EventDate: reader.GetDateTime(3),
                    CreatedAt: reader.GetDateTime(4)));
            }
        }
        model.RecentEvents = recent;

        return View(model);
    }
}
