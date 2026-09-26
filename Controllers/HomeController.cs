using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // Kjøres når skjemaet sendes inn, lagrer ressursen og sender videre til Resources siden
    [HttpPost]
    public IActionResult RegisterResource(ResourceEntry resource)
    {
        ResourceStore.Add(resource);
        return RedirectToAction("Index", "Resources");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
