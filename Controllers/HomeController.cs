using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace H_st_2026_Gruppe_1.Controllers;

public class HomeController : Controller
{
    public IActionResult Index(int? id)
    {
        if (id.HasValue)
        {
            var existingResource = ResourceStore.GetById(id.Value);
            if (existingResource != null)
            {
                return View(existingResource);
            }
        }
        
        // Sends an empty model to the form the first time the page is opened.
        return View(new ResourceEntry());     
    }

    // Runs when the form is submitted, saves the resource, and redirects to the Resources page.
    [HttpPost]
    public IActionResult RegisterResource(ResourceEntry resource)
    {
        // 1. Beregn utløpsdato dersom den ikke er satt manuelt
        if (resource.ExpirationDate == null && !string.IsNullOrWhiteSpace(resource.ResourceType))
        {
            resource.ExpirationDate = GetExpiryDate(resource.ResourceType);
        }

        // 2. Sjekk om skjemaet er gyldig FØR vi lagrer
        if (!ModelState.IsValid)
        {
            return View("Index", resource);
        }

        // 3. Lagre eller oppdater
        if (resource.Id > 0)
        {
            ResourceStore.Update(resource);
        }
        else
        {
            ResourceStore.Add(resource);
        }

        return RedirectToAction("Index", "Resources");
    }

    // Calculates the expiry date based on the resource category.
    private static DateTime? GetExpiryDate(string resourceType)
    {
        return resourceType switch
        {
            "Shelter" or "Transport" or "Other" => DateTime.Today.AddYears(1),
            "Food" => DateTime.Today.AddDays(14),
            "Medical" => DateTime.Today.AddMonths(1),
            "Materials" => DateTime.Today.AddMonths(6), // Fikset skrivefeil
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

    [HttpPost]
    public IActionResult DeleteResource(int id)
    {
        ResourceStore.Remove(id);
        return RedirectToAction("index", "Resources");
    }
}
