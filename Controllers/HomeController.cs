using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Sends an empty model to the form the first time the page is opened.
        return View(new ResourceEntry());
    }

    // Runs when the form is submitted, saves the resource, and redirects to the Resources page.
    [HttpPost]
    public IActionResult RegisterResource(ResourceEntry resource)
    {
        // Calculates the date before the resource is saved.
        resource.ExpiryDate = GetExpiryDate(resource.ResourceType);

        if (!ModelState.IsValid)
        {
            // Sends the model back so the entered values are retained in the form.
            return View("Index", resource);
        }

        ResourceStore.Add(resource);
        return RedirectToAction("Index", "Resources");
    }

    // Calculates the expiry date based on the resource category.
    private static DateTime? GetExpiryDate(string resourceType)
    {
        return resourceType switch
        {
            "Shelter" or "Transport" or "Annet" => DateTime.Today.AddYears(1),
            "Mat" => DateTime.Today.AddDays(14),
            "Medisinsk" => DateTime.Today.AddMonths(1),
            "Materialer" => DateTime.Today.AddMonths(6),
            _ => null
        };
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
