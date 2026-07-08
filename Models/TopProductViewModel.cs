namespace MultiAreaPortal.Models;

// One row of the "Top products by quantity sold" report.
// Populated identically by the raw ADO.NET stored-procedure path and the EF Core LINQ path.
public class TopProductViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
}
