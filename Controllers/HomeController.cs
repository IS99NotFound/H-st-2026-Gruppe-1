using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace H_st_2026_Gruppe_1.Controllers;

public class PublicActorController : Controller
{
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }
    
    private readonly AppDbContext _db;
    private readonly MatchNeedLifecycle _lifecycle;

    public PublicActorController (AppDbContext db, MatchNeedLifecycle lifecycle)
    {
        _db = db;
        _lifecycle = lifecycle;
    }
    private void LoadResourceMarkers()
{
    var resources = _db.Resources
        .Include(r => r.Category)
        .Where(r =>
            r.Status == "available" &&
            r.Latitude.HasValue &&
            r.Longitude.HasValue)
        .Select(r => new
        {
            name = r.Name,
            resourceType = r.Category != null
                ? r.Category.CategoryName
                : "Other",
            description = r.Description,
            phone = r.PhoneNumber,
            latitude = r.Latitude,
            longitude = r.Longitude
        })
        .ToList();

    ViewBag.ResourceMarkersJson =
        System.Text.Json.JsonSerializer.Serialize(resources);
}

    // Categories for the dropdown
    private void LoadDropdowns()
    {
        ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();
        ViewBag.Incidents = _db.Incidents.OrderBy(i => i.IncidentId).ToList();
    }
    
    public IActionResult DashboardIndex(int? id)
    {
        if (!CanRegisterNeed())
        {
            return RedirectToAction("Index", "Home");
        }

        LoadDropdowns();
        LoadResourceMarkers();
        LoadOperationalFeed();
        if (id.HasValue)
        {
            var need = _db.Needs.Find(id.Value);

            if (need == null)
            {
                return RedirectToAction(nameof(MatchesIndex));
            }

            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            if (!User.IsInRole("Administrator") && need.PublicActorId != currentUserId.Value)
            {
                return Forbid();
            }

            return View(need);
        }

        return View(new ResourceNeed());
    }

    private void LoadOperationalFeed()
    {
        ViewBag.FeedResources = _db.Resources
            .Include(resource => resource.Category)
            .AsEnumerable()
            .Select(resource => new OperationalFeedItem
            {
                Type = "resource",
                Id = resource.Id,
                Title = resource.Name,
                Description = resource.Description,
                Status = resource.Status,
                Category = resource.Category?.CategoryName,
                ColorKey = "available-resource",
                Latitude = resource.Latitude ?? 0,
                Longitude = resource.Longitude ?? 0,
                CreatedAt = resource.RegisteredAt
            })
            .ToList();
        ViewBag.FeedNeeds = _db.Needs
            .Include(need => need.CategoryInfo)
            .AsEnumerable()
            .Select(need => new OperationalFeedItem
            {
                Type = "need",
                Id = need.Id,
                Title = need.Title,
                Description = need.Description,
                Status = need.Status,
                Category = need.CategoryInfo?.CategoryName,
                Priority = need.Priority.ToString(),
                ColorKey = need.Priority == NeedPriorityEnum.Critical
                    ? "critical-need"
                    : "planned-need",
                Latitude = need.Latitude,
                Longitude = need.Longitude,
                CreatedAt = need.RegisteredAt
            })
            .ToList();
        ViewBag.FeedIncidents = _db.Incidents
            .AsEnumerable()
            .Select(incident => new OperationalFeedItem
            {
                Type = "incident",
                Id = incident.IncidentId,
                Title = incident.Title,
                Description = incident.Description,
                Status = incident.Status,
                ColorKey = "public-team",
                Latitude = (double)incident.Latitude,
                Longitude = (double)incident.Longitude,
                CreatedAt = incident.CreatedAt
            })
            .ToList();
    }
    
    
    [Authorize(Roles = "PublicActor,Administrator")]
    public IActionResult NeedsIndex(
        NeedPriorityEnum? priority,
        int? category,
        DateTime? startDate,
        DateTime? endDate,
        int? id)
    {
        _lifecycle.ReleaseFinishedAssignments();
        
        var results = _db.Needs.Include(need => need.CategoryInfo).AsQueryable();

        if (!User.IsInRole("Administrator"))
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            results = results.Where(need => need.PublicActorId == currentUserId.Value);
        }

        if (priority.HasValue)
        {
            results = results.Where(need => need.Priority == priority.Value);
        }

        if (category.HasValue)
        {
            results = results.Where(need => need.Category == category.Value);
        }

        if (startDate.HasValue)
        {
            results = results.Where(need =>
                need.RequiredFrom.HasValue &&
                need.RequiredFrom.Value >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            results = results.Where(need =>
                need.RequiredFrom.HasValue &&
                need.RequiredFrom.Value < endDate.Value.Date.AddDays(1));
        }

        // GetPriorityOrder cannot be translated to SQL, so the list is fetched first and sorted in memory
        var sortedResults = results
            .ToList()
            .OrderBy(need => GetPriorityOrder(need.Priority))
            .ToList();

        ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();
        ViewBag.Incidents = _db.Incidents.OrderBy(i => i.IncidentId).ToList();
        if (id.HasValue)
        {
            var editNeed = sortedResults.FirstOrDefault(need => need.Id == id.Value);
            if (editNeed == null)
            {
                return NotFound();
            }

            ViewBag.EditNeed = editNeed;
        }
        return View(sortedResults);
    }
    
    private int GetPriorityOrder(NeedPriorityEnum priority)
    {
        return priority switch
        {
            NeedPriorityEnum.Critical => 1,
            NeedPriorityEnum.Planned => 2,
            _ => 3
        };
    }

    private bool CanRegisterNeed()
    {
        return User.IsInRole("PublicActor") || User.IsInRole("Administrator");
    }


    [HttpPost] 
    [Authorize(Roles = "PublicActor,Administrator")] 
    [ValidateAntiForgeryToken]
    public IActionResult RegisterNeed(ResourceNeed need)
    {
        if (!CanRegisterNeed())
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            LoadResourceMarkers();
            LoadOperationalFeed();
            ViewBag.OpenRegistrationModal = true;
            return View("DashboardIndex", need);
        }
        // Save to database
        need.RegisteredAt = DateTime.Now;
        if (need.Id > 0)
        {
            var existingNeed = _db.Needs.Find(need.Id);

            if (existingNeed == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            if (!User.IsInRole("Administrator") &&
                existingNeed.PublicActorId != currentUserId.Value)
            {
                return Forbid();
            }
            
            if (existingNeed != null)
            {
                existingNeed.Title = need.Title;
                existingNeed.Email = need.Email;
                existingNeed.PhoneNumber = need.PhoneNumber;
                existingNeed.LocationName = need.LocationName;
                existingNeed.CrisisEventId = need.CrisisEventId;
                existingNeed.Latitude = need.Latitude;
                existingNeed.Longitude = need.Longitude;
                existingNeed.Category = need.Category;
                existingNeed.Description = need.Description;
                existingNeed.Quantity = need.Quantity;
                existingNeed.RequiredFrom = need.RequiredFrom;
                existingNeed.Deadline =need.Deadline;
                existingNeed.Priority = need.Priority;
                existingNeed.RegisteredAt = DateTime.Now;
               // existingNeed.Status = need.Status; 
               // <- Bug, no status-field. After changes, status will always be new
                _db.SaveChanges();
            }
        }
        else
        {
            if (!User.IsInRole("PublicActor"))
            {
                return Forbid();
            }

            var currentUserId = GetCurrentUserId();

            if (currentUserId is null)
            {
                return Unauthorized();
            }

            need.PublicActorId = currentUserId.Value;
            need.RegisteredAt = DateTime.Now;
            _db.Needs.Add(need);
            _db.SaveChanges();
        }
        _db.SaveChanges();
        // Show the resources near the new need
        return RedirectToAction("NeedsIndex");
    }

    public IActionResult MatchesIndex(int? needId, double radiusKm = 0)
    {
        return View("MatchesIndex", BuildMatchViewModel(needId, radiusKm));
    }
    
    // Shows the resources within radiusKm of a need, nearest first.
    public IActionResult Matches(int? needId, double radiusKm = 0)
    {
        var model = BuildMatchViewModel(needId, radiusKm);
        if (model.Need.Id == 0)
        {
            return RedirectToAction(nameof(MatchesIndex));
        }

        return View("MatchesIndex", model);
    }
    
    public IActionResult ShowAllResourcesInMatches(int needId, double radiusKm = 0)
    {
        var model = BuildMatchViewModel(needId, radiusKm, allCategories: true);
        if (model.Need.Id == 0)
        {
            return NotFound();
        }

        return View("MatchesIndex", model);

    }

    private MatchViewModel BuildMatchViewModel(int? needId, double radiusKm, bool allCategories = false)
    {
       //Will free room for resources if a need is set as resolved or deadline has passed
        _lifecycle.ReleaseFinishedAssignments();

        var needs = _db.Needs.Include(need => need.CategoryInfo).ToList();
        var selectedNeed = needId.HasValue
            ? needs.FirstOrDefault(need => need.Id == needId.Value)
            : needs.OrderByDescending(need => need.Id).FirstOrDefault();

        if (!double.IsFinite(radiusKm) || radiusKm < 0)
        {
            radiusKm = 0;
        }

        var model = new MatchViewModel
        {
            Need = selectedNeed ?? new ResourceNeed(),
            RadiusKm = radiusKm,
            AllCategories = allCategories,
            UserNeeds = needs
        };

        if (selectedNeed == null)
        {
            return model;
        }

         var records = _db.MatchRecords
            .Where(match => match.NeedId == selectedNeed.Id)
            .ToList();
           
        var assignedToThisNeed = records
            .Where(record => record.Status == "accepted")
            .Select(record => record.ResourceId)
            .ToList();

        model.Matches = RadiusMatcher.FindResources(
            selectedNeed,
            radiusKm,
            _db.Resources
            .Include(resource => resource.Category)
            .Where(resource => resource.Status == "available" || assignedToThisNeed.Contains(resource.Id))
            .ToList(),
            allCategories);
 
        foreach (var match in model.Matches)
        {
            var record = records.FirstOrDefault(item => item.ResourceId == match.Resource.Id);
            match.MatchStatus = record?.Status;
            match.Comment = record?.Comment;
        }

        return model;
    }

    // Saves the decision about a resource for a need in the ResourceMatch table.
    // newStatus: accepted = assigned, proposed = under review, declined = rejected
    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateResourceStatus(int resourceId, int needId, string newStatus, string? statusComment)
    {
        if (newStatus is not ("proposed" or "accepted" or "declined"))
        {
            return BadRequest();
        }

        var need = _db.Needs.Find(needId);
        if (need == null || !_db.Resources.Any(resource => resource.Id == resourceId))
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        if (need.PublicActorId != currentUserId.Value)
        {
            return Forbid();
        }

        var comment = string.IsNullOrWhiteSpace(statusComment) ? null : statusComment.Trim();
        if (comment != null && comment.Length > 500)
        {
            comment = comment.Substring(0, 500);
        }

        var record = _db.MatchRecords.Find(needId, resourceId);
        var wasAssigned = record?.Status == "accepted";

        //Assign: lock the resource FIRST. If someone else took it first, nothing is saved.
        if (newStatus == "accepted" && !wasAssigned)
        {
            if (need.Status is "resolved" or "cancelled")
            {
                TempData["MatchError"] = "This need is closed, so resources can no longer be assigned to it.";
                return RedirectToAction("Matches", new { needId });
            }

            if (!_lifecycle.TryReserveResource(resourceId))
            {
                TempData["MatchError"] = "This resource is temporarily unavailable. It is already assigned to another need.";
                 return RedirectToAction("Matches", new { needId });
            }
        }
        if (record == null)
        {
            record = new MatchRecord
            {
                NeedId = needId,
                ResourceId = resourceId
            };
            _db.MatchRecords.Add(record);
        }

        record.PublicActorId = need.PublicActorId;
        record.Status = newStatus;
        record.Comment = comment;
        record.MatchDate = DateTime.Now;
        _db.SaveChanges();

        if (wasAssigned && newStatus != "accepted")
        {
            _lifecycle.ReleaseResource(resourceId);
        }

        _lifecycle.RefreshNeedStatus(needId);

        TempData["MatchMessage"] = newStatus switch
        {
            "accepted" => "Resource assigned. It is now marked as temporarily unavailable for other public actors.",
            "proposed" when wasAssigned => "Resource set to under review and is available again.",
            "declined" when wasAssigned => "Resource rejected and is available again",
            _ => "Status saved."
        };

        return RedirectToAction("Matches", new { needId });
    }
    public IActionResult ResolveNeed(int id)
    {
        var need = _db.Needs.Find(id);
        if (need == null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        if (!User.IsInRole("Administrator") && need.PublicActorId != currentUserId.Value)
        {
            return Forbid();
        }

        _lifecycle.CloseNeed(id, "resolved");
        return RedirectToAction(nameof(NeedsIndex));
    }
}



public class HomeController : Controller

{
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }
    
    private readonly AppDbContext _db;
    private readonly MatchNeedLifecycle _lifecycle;

    public HomeController(AppDbContext db, MatchNeedLifecycle lifecycle)
    {
        _db = db;
        _lifecycle = lifecycle;
    }

    [Authorize(Roles = "CivilianProvider,Administrator")]
    public IActionResult Index(int? id)
    {
        if (!CanRegisterResource())
        {
            return RedirectToAction("DashboardIndex", "PublicActor");
        }

        ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();
        LoadOperationalFeed();

        if (id.HasValue)
        {
            var existingResource = _db.Resources.Find(id.Value);
            if (existingResource != null)
            {
                var currentUserId = GetCurrentUserId();

                if (currentUserId is null)
                {
                    return Unauthorized();
                }

                if (!User.IsInRole("Administrator") &&
                    existingResource.ProviderId != currentUserId.Value)
                {
                    return Forbid();
                }
            }
            return View(existingResource);

        }
        
        // Sends an empty model to the form the first time the page is opened.
        return View(new ResourceEntry());     
    }

    private void LoadOperationalFeed()
    {
        ViewBag.FeedResources = _db.Resources
            .Include(resource => resource.Category)
            .AsEnumerable()
            .Select(resource => new OperationalFeedItem
            {
                Type = "resource",
                Id = resource.Id,
                Title = resource.Name,
                Description = resource.Description,
                Status = resource.Status,
                Category = resource.Category?.CategoryName,
                ColorKey = "available-resource",
                Latitude = resource.Latitude ?? 0,
                Longitude = resource.Longitude ?? 0,
                CreatedAt = resource.RegisteredAt
            })
            .ToList();
        ViewBag.FeedNeeds = _db.Needs
            .Include(need => need.CategoryInfo)
            .AsEnumerable()
            .Select(need => new OperationalFeedItem
            {
                Type = "need",
                Id = need.Id,
                Title = need.Title,
                Description = need.Description,
                Status = need.Status,
                Category = need.CategoryInfo?.CategoryName,
                Priority = need.Priority.ToString(),
                ColorKey = need.Priority == NeedPriorityEnum.Critical
                    ? "critical-need"
                    : "planned-need",
                Latitude = need.Latitude,
                Longitude = need.Longitude,
                CreatedAt = need.RegisteredAt
            })
            .ToList();
        ViewBag.FeedIncidents = _db.Incidents
            .AsEnumerable()
            .Select(incident => new OperationalFeedItem
            {
                Type = "incident",
                Id = incident.IncidentId,
                Title = incident.Title,
                Description = incident.Description,
                Status = incident.Status,
                ColorKey = "public-team",
                Latitude = (double)incident.Latitude,
                Longitude = (double)incident.Longitude,
                CreatedAt = incident.CreatedAt
            })
            .ToList();
    }
    
    
    // Runs when the form is submitted, saves the resource, and redirects to the Resources page.
    [HttpPost]
    [Authorize(Roles = "CivilianProvider,Administrator")]  
    [ValidateAntiForgeryToken] 
    public IActionResult RegisterResource(ResourceEntry resource)
    {
        if (!CanRegisterResource())
        {
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            // sends the user to form with attributes and error messages filled out
            ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();
            LoadOperationalFeed();
            ViewBag.OpenRegistrationModal = true;
            return View("Index", resource);
        }
        
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        //save and update only when evrything is valid
        if (resource.Id > 0)
        {
            var existingResource = _db.Resources.Find(resource.Id);
            if (existingResource != null)
            {
                if (!User.IsInRole("Administrator") &&
                    existingResource.ProviderId != currentUserId.Value)
                {
                    return Forbid();
                }
                
                existingResource.Name = resource.Name;
                existingResource.Description = resource.Description;
                existingResource.CategoryId = resource.CategoryId;
                existingResource.Latitude = resource.Latitude;
                existingResource.Longitude = resource.Longitude;
                existingResource.Email = resource.Email;
                existingResource.PhoneNumber = resource.PhoneNumber;
                existingResource.LocationName = resource.LocationName;
                existingResource.ExpirationDate = resource.ExpirationDate;
                existingResource.AvailableFrom = resource.AvailableFrom;
                existingResource.RegisteredAt = DateTime.Now;
            }
        }
        else
        {
            if (!User.IsInRole("CivilianProvider"))
            {
                return Forbid();
            }
            
            
            resource.ProviderId = currentUserId.Value;
            resource.RegionId = DemoDefaults.RegionId;
            resource.RegisteredAt = DateTime.Now;
            _db.Resources.Add(resource);
        }
        _db.SaveChanges();
        return RedirectToAction("Index", "Resources");
    }

    [Authorize(Roles = "CivilianProvider, Administrator")]
    private bool CanRegisterResource()
    {
        return User.IsInRole("CivilianProvider") || User.IsInRole("Administrator");
    }
    

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [Authorize(Roles = "CivilianProvider, Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResource(int id)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }
        
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }
        
        bool isAdmin = User.IsInRole("Administrator");

        if (!isAdmin && resource.ProviderId != (currentUserId))
        {
            return Forbid();
        }
        
        _db.Resources.Remove(resource);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "PublicActor, Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNeed(int id)
    {
        var Need = await _db.Needs.FindAsync(id);
        if (Need == null)
        {
            return NotFound();
        }
        
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }
        
        bool isAdmin = User.IsInRole("Administrator");

        if (!isAdmin && Need.PublicActorId != (currentUserId))
        {
            return Forbid();
        }

        // Fjern match-radene først (fremmednøkkel) og frigi assigned ressurser
        _lifecycle.RemoveMatchesFromNeed(Need.Id);

        _db.Needs.Remove(Need);
        await _db.SaveChangesAsync();
        return RedirectToAction("NeedsIndex", "PublicActor");    }
    
}
