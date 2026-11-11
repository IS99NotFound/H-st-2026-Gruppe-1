using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Helpers;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.AspNetCore.Authorization;

namespace H_st_2026_Gruppe_1.Controllers;


public class ResourcesController : Controller
{
    private readonly AppDbContext _db;
    private readonly MatchNeedLifecycle _lifecycle;

    public ResourcesController(AppDbContext db, MatchNeedLifecycle lifecycle)
    {
        _db = db;
        _lifecycle = lifecycle;
    }

    public IActionResult Index(
        int? id,
        string? search,
        string? status,
        int? resourceType,
        DateTime? startDate,
        DateTime? endDate)
    
    {
        // Today and the date 5 days ahead. They are used to find resources that expire soon.
        var now = DateTime.Today;
        var fiveDaysAhead = now.AddDays(5);
        var resourcesQuery = _db.Resources
            .Include(resource => resource.Category)
            .AsQueryable();

        // A civilian provider may only see the resources they registered themselves.
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

        ViewBag.Categories = _db.Categories
            .Where(category => _db.Resources.Any(resource => resource.CategoryId == category.CategoryId))
            .OrderBy(category => category.CategoryName)
            .ToList();
        ViewBag.ResourceCategories = _db.Categories
            .AsEnumerable()
            .Where(category => ResourceCategoryPolicy.IsAllowed(category.CategoryName))
            .OrderBy(category => category.CategoryId)
            .ToList();

        var resourceIds = allResources.Select(resource => resource.Id).ToList();

        var matchStatus = new List<ResourceMatchStatusViewModel>();
        if (User.IsInRole("CivilianProvider") || User.IsInRole("Administrator"))
        {
            var today = DateTime.Now;

            // Tildelte matcher, og forespørsler (proposed) på behov som fortsatt er åpne
            matchStatus = (from match in _db.MatchRecords
                join need in _db.Needs on match.NeedId equals need.Id
                where resourceIds.Contains(match.ResourceId)
                    && (match.Status == "accepted" || match.Status == "assigned"
                        || (match.Status == "proposed"
                            && need.Status != "resolved" && need.Status != "cancelled"
                            && (need.Deadline == null || need.Deadline >= today)))
                select new ResourceMatchStatusViewModel()
                {
                    ResourceId = match.ResourceId,
                    NeedId = need.Id,
                    NeedTitle = need.Title,
                    NeedLocation = need.LocationName,
                    NeedDeadline = need.Deadline,
                    NeedPriority = need.Priority,
                    NeedEmail = need.Email,
                    NeedPhone = need.PhoneNumber,
                    Status = match.Status,
                    Comment = match.Comment
                }).ToList();
        }
        
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
            Search = search,
            Status = status,
            ResourceType = resourceType,
            StartDate = startDate,
            MatchStatuses = matchStatus,
            EndDate = endDate
            
        };
        

        // If an id is in the address, open that resource for editing.
        if (id.HasValue)
        {
            if (!User.IsInRole("Administrator") && !User.IsInRole("CivilianProvider"))
            {
                return Forbid();
            }

            var resource = allResources.FirstOrDefault(item => item.Id == id.Value);
            if (resource == null)
            {
                return NotFound();
            }

            ViewBag.EditResource = resource;
        }

        return View(dashboardModel);

    }

    // The provider answers a request from a public actor: accept or decline.
    [HttpPost]
    [Authorize(Roles = "CivilianProvider")]
    [ValidateAntiForgeryToken]
    public IActionResult RespondToMatch(int resourceId, int needId, string answer)
    {
        if (answer is not ("accepted" or "declined"))
        {
            return BadRequest();
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
        {
            return Unauthorized();
        }

        var providerId = _db.Resources
            .Where(resource => resource.Id == resourceId)
            .Select(resource => (int?)resource.ProviderId)
            .FirstOrDefault();
        if (providerId == null)
        {
            return NotFound();
        }

        if (providerId != currentUserId)
        {
            return Forbid();
        }

        var need = _db.Needs.Find(needId);
        var record = _db.MatchRecords.Find(needId, resourceId);
        if (need == null || record == null)
        {
            return NotFound();
        }

        if (record.Status != "proposed")
        {
            TempData["ResourceError"] = "Denne forespørselen kan ikke lenger besvares.";
            return RedirectToAction("Index");
        }

        if (need.Status is "resolved" or "cancelled"
            || need.Deadline.HasValue && need.Deadline.Value < DateTime.Now)
        {
            TempData["ResourceError"] = "Behovet er lukket eller fristen har gått ut.";
            return RedirectToAction("Index");
        }

        // Lock the resource first. It fails if the resource is taken or withdrawn.
        if (answer == "accepted" && !_lifecycle.TryReserveResource(resourceId))
        {
            TempData["ResourceError"] = "Ressursen er ikke ledig. Den er allerede tildelt et annet behov, eller trukket tilbake.";
            return RedirectToAction("Index");
        }

        record.Status = answer;
        record.MatchDate = DateTime.Now;
        _db.SaveChanges();

        _lifecycle.RefreshNeedStatus(needId);

        TempData["SuccessMessage"] = answer == "accepted"
            ? "Du har godtatt forespørselen. Ressursen er nå tildelt."
            : "Du har avslått forespørselen.";
        return RedirectToAction("Index");
    }
}
