-- Aggregation stored procedure for the "Top products by quantity sold" report.
-- Returns the top-N products by total quantity, with optional date-range and
-- minimum-quantity filters. Consumed by the raw ADO.NET path in ReportsService.
CREATE OR ALTER PROCEDURE dbo.usp_GetTopProductsByQuantity
    @Top         INT  = 10,
    @StartDate   DATE = NULL,   -- optional inclusive start of order-date range
    @EndDate     DATE = NULL,   -- optional inclusive end of order-date range
    @MinQuantity INT  = 0       -- exclude products whose total quantity is below this
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        p.Id                            AS ProductId,
        p.Name                          AS ProductName,
        c.Name                          AS CategoryName,
        SUM(oi.Quantity)                AS TotalQuantitySold,
        SUM(oi.UnitPrice * oi.Quantity) AS TotalSalesAmount
    FROM OrderItems  oi
        INNER JOIN Orders     o ON o.Id = oi.OrderId
        INNER JOIN Products   p ON p.Id = oi.ProductId
        INNER JOIN Categories c ON c.Id = p.CategoryId
    WHERE (@StartDate IS NULL OR o.OrderDate >= @StartDate)
      AND (@EndDate   IS NULL OR o.OrderDate <  DATEADD(DAY, 1, @EndDate))
    GROUP BY p.Id, p.Name, c.Name
    HAVING SUM(oi.Quantity) >= @MinQuantity
    ORDER BY TotalQuantitySold DESC;
END
