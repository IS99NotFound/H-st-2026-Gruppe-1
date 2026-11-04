using System.Security.Claims;
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

    public IActionResult Index(
        int? id,
        string? search,
        string? status,
        int? resourceType,
        DateTime? startDate,
        DateTime? endDate)
    {
        var now = DateTime.Today;
        var fiveDaysAhead = now.AddDays(5);
        var resourcesQuery = _db.Resources
            .Include(resource => resource.Category)
            .AsQueryable();

        if (User.IsInRole("CivilianProvider"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized();
            }

            resourcesQuery = resourcesQuery
                .Where(resource => resource.ProviderId == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            resourcesQuery = resourcesQuery.Where(resource =>
                resource.Name.Contains(search) ||
                resource.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            resourcesQuery = resourcesQuery.Where(resource => resource.Status == status);
        }

        if (resourceType.HasValue)
        {
            resourcesQuery = resourcesQuery.Where(resource => resource.CategoryId == resourceType.Value);
        }

        if (startDate.HasValue)
        {
            resourcesQuery = resourcesQuery.Where(resource => resource.AvailableFrom >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            resourcesQuery = resourcesQuery.Where(resource =>
                resource.AvailableFrom < endDate.Value.Date.AddDays(1));
        }

        var allResources = resourcesQuery
            .OrderBy(resource => resource.Id)
            .ToList();

        ViewBag.Categories = _db.Categories.OrderBy(category => category.CategoryId).ToList();
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
            Resources = allResources,
            Resources = allResources,
            Search = search,
            Status = status,
            ResourceType = resourceType,
            StartDate = startDate,
            EndDate = endDate
        };

        if (id.HasValue)
        {
            var resource = allResources.FirstOrDefault(item => item.Id == id.Value);
            if (resource == null)
            {
                return NotFound();
            }

            ViewBag.EditResource = resource;
        }

        return View(dashboardModel);
    }
}
