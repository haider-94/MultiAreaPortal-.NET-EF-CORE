using System.ComponentModel.DataAnnotations;
using MultiAreaPortal.Models;

namespace MultiAreaPortal.Areas.Admin.Models;

// Backs the Admin "Top Products" reporting page: filter inputs, result rows, and timing metrics.
public class TopProductsReportViewModel
{
    [Range(1, 100)]
    [Display(Name = "Top N")]
    public int Top { get; set; } = 10;

    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "End date")]
    public DateTime? EndDate { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Min quantity")]
    public int MinQuantity { get; set; } = 0;

    // How many timed iterations to run per method (excludes the warm-up call).
    [Range(1, 50)]
    [Display(Name = "Benchmark runs")]
    public int Runs { get; set; } = 10;

    public bool HasRun { get; set; }

    public IReadOnlyList<TopProductViewModel> AdoNetResults { get; set; } = [];
    public IReadOnlyList<TopProductViewModel> EfCoreResults { get; set; } = [];

    public BenchmarkResult? AdoNetTiming { get; set; }
    public BenchmarkResult? EfCoreTiming { get; set; }

    // True when both paths returned an identical result set (parity check).
    public bool ResultsMatch { get; set; }
}

public record BenchmarkResult(string Method, int Runs, double AverageMs, double MinMs, double MaxMs);
