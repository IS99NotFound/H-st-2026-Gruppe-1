using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Controllers;

public class ResourcesController : Controller
{
    private readonly AppDbContext _db;

    public ResourcesController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        var now = DateTime.Today;
        var fiveDaysAhead = now.AddDays(5);
        var allResources = _db.Resources.Include(r => r.Category).OrderBy(r => r.Id).ToList();
        var dashboardModel = new ResourceDashboard
        {
            TotalResources = allResources.Count,

            // Resources that expires today or in 5 days 
            ExpiringSoonCount = allResources.Count(r =>
                r.ExpirationDate.HasValue &&
                r.ExpirationDate.Value.Date >= now &&
                r.ExpirationDate.Value.Date <= fiveDaysAhead),

            // Resource already expired before today 
            ExpiredCount = allResources.Count(r =>
                r.ExpirationDate.HasValue &&
                r.ExpirationDate.Value.Date < now),

            Resources = allResources
        };

        // 3. Send dashboard-model to View
        return View(dashboardModel);
    }
}

