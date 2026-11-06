using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace H_st_2026_Gruppe_1.Controllers;

// Placeholder controller for the reports page. It only shows the view for now.
public class ReportsController : Controller
{
    [Authorize(Roles = "PublicActor")]
    public IActionResult Index()
    {
        return View();
    }
}