using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MultiAreaPortal.Areas.Public.Controllers;

// Public landing page — accessible to anonymous visitors (no authentication required).
[Area("Public")]
[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
