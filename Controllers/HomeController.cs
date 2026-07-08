using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MultiAreaPortal.Models;

namespace MultiAreaPortal.Controllers;

// Root entry point. Sends visitors to the anonymous-accessible Public area landing page.
public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Home", new { area = "Public" });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
