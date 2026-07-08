using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiAreaPortal.Areas.Admin.Models;
using MultiAreaPortal.Models;
using MultiAreaPortal.Services;

namespace MultiAreaPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReportsController(ReportsService reports) : Controller
{
    // GET: /Admin/Reports/TopProducts — empty form.
    [HttpGet]
    public IActionResult TopProducts() => View(new TopProductsReportViewModel());

    // POST: /Admin/Reports/TopProducts — run both data-access paths, compare results + timing.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopProducts(TopProductsReportViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        Task<List<TopProductViewModel>> AdoNet(CancellationToken c) =>
            reports.GetTopProductsAdoNetAsync(model.Top, model.StartDate, model.EndDate, model.MinQuantity, c);

        Task<List<TopProductViewModel>> EfCore(CancellationToken c) =>
            reports.GetTopProductsEfCoreAsync(model.Top, model.StartDate, model.EndDate, model.MinQuantity, c);

        model.AdoNetResults = await AdoNet(ct);
        model.EfCoreResults = await EfCore(ct);
        model.ResultsMatch = ResultsAreEqual(model.AdoNetResults, model.EfCoreResults);

        model.AdoNetTiming = await reports.BenchmarkAsync("ADO.NET (stored procedure)", AdoNet, model.Runs, ct);
        model.EfCoreTiming = await reports.BenchmarkAsync("EF Core (LINQ)", EfCore, model.Runs, ct);

        model.HasRun = true;
        return View(model);
    }

    private static bool ResultsAreEqual(
        IReadOnlyList<TopProductViewModel> a, IReadOnlyList<TopProductViewModel> b)
    {
        if (a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (a[i].ProductId != b[i].ProductId
                || a[i].TotalQuantitySold != b[i].TotalQuantitySold
                || a[i].TotalSalesAmount != b[i].TotalSalesAmount)
                return false;
        }
        return true;
    }
}
