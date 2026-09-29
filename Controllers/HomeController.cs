using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace H_st_2026_Gruppe_1.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Sender en tom modell til skjemaet første gang siden åpnes
        return View(new ResourceViewModel());
    }

    // Kjører når skjemaet sendes inn, lagrer ressursen og sender brukeren til Resources-siden
    [HttpPost]
    public IActionResult RegisterResource(ResourceViewModel resource)
    {
        // Sjekker valideringsreglene i ResourceViewModel
        if (!ModelState.IsValid)
        {
            return View("Index", resource);
        }

        // Regner ut utløpsdatoen før ressursen lagres
        resource.ExpiryDate = GetExpiryDate(resource.ResourceType);
        if (!string.IsNullOrWhiteSpace(resource.ResourceType) && resource.ExpiryDate is null)
        {
            ModelState.AddModelError(nameof(resource.ResourceType), "Select a resource type.");
        }

        if (!ModelState.IsValid)
        {
            // Sender modellen tilbake slik at det brukeren skrev inn beholdes i skjemaet
            return View("Index", resource);
        }

        // Flytter verdiene fra skjemamodellen over i ResourceEntry, som er det som lagres
        var entry = new ResourceEntry
        {
            Name = resource.Name,
            Phone = resource.Phone,
            Description = resource.Description,
            ResourceType = resource.ResourceType,
            ExpiryDate = resource.ExpiryDate,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude
        };

        ResourceStore.Add(entry);
        return RedirectToAction("Index", "Resources");
    }


    // Regner ut utløpsdato ut fra ressurstypen
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

    // Viser personvernsiden
    public IActionResult Privacy()
    {
        return View();
    }

    // Viser feilsiden
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}