namespace MultiAreaPortal.Areas.Customer.Models;

// Row shape returned by the raw ADO.NET reader (not an EF entity).
public record RecentEvent(int Id, string Name, decimal TicketPrice, DateTime EventDate, DateTime CreatedAt);

public class DashboardViewModel
{
    public int TotalEvents { get; set; }
    public IReadOnlyList<RecentEvent> RecentEvents { get; set; } = [];
}
