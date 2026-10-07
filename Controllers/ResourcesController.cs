using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Controllers;

public class ResourcesController : Controller
{
    public IActionResult Index()
    {
        var now = DateTime.Today;
        var fiveDaysAhead = now.AddDays(5);
        
        var allResources = ResourceStore.Resources;
        var dashboardModel = new ResourceDashboard
        {
            TotalResources = allResources.Count,

            // Ressurser som utløper mellom i dag og 5 dager frem i tid
            ExpiringSoonCount = allResources.Count(r =>
                r.ExpirationDate.HasValue &&
                r.ExpirationDate.Value.Date >= now &&
                r.ExpirationDate.Value.Date <= fiveDaysAhead),

            // Ressurser som allerede har utløpt (før i dag)
            ExpiredCount = allResources.Count(r =>
                r.ExpirationDate.HasValue &&
                r.ExpirationDate.Value.Date < now),

            Resources = allResources
        };

        // 3. Send dashboard-modellen til Viewet
        return View(dashboardModel);
    }
}

