using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Controllers;

public class ResourcesController : Controller
{
    // Viser alt som er registrert via skjemaet
    public IActionResult Index()
    {
        return View(ResourceStore.Resources);
    }
}