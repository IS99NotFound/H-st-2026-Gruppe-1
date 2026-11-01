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

    public IActionResult Index(int? id)
    {
        var now = DateTime.Today;
        var fiveDaysAhead = now.AddDays(5);
        ViewBag.Categories = _db.Categories.OrderBy(category => category.CategoryId).ToList();
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

        if (id.HasValue)
        {
            var resource = allResources.FirstOrDefault(item => item.Id == id.Value);
            if (resource == null)
            {
                return NotFound();
            }

            var contactParts = (resource.Phone ?? string.Empty)
                .Split(" / ", 2, StringSplitOptions.TrimEntries);
            resource.Email = contactParts.ElementAtOrDefault(0) ?? string.Empty;
            resource.PhoneNumber = contactParts.ElementAtOrDefault(1) ?? "+47 ";
            ViewBag.EditResource = resource;
        }

        return View(dashboardModel);
    }
}
