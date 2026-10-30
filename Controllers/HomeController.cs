using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Controllers;

public class PublicActorController : Controller
{
    private readonly AppDbContext _db;

    public PublicActorController (AppDbContext db)
    {
        _db = db;
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
            phone = r.Phone,
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
        LoadDropdowns();
        LoadResourceMarkers();
        if (id.HasValue)
        {
            var need = _db.Needs.Find(id.Value);
            if (need == null)
            {
                return NotFound();
            }
            return View(need);
        }

        return View(new ResourceNeed());
    }
    
    
    public IActionResult NeedsIndex(NeedPriorityEnum? priority, int? category)
    {
        var results = _db.Needs.Include(need => need.CategoryInfo).AsQueryable();

        if (priority.HasValue)
        {
            results = results.Where(need => need.Priority == priority.Value);
        }

        if (category.HasValue)
        {
            results = results.Where(need => need.Category == category.Value);
        }

        // GetPriorityOrder cannot be translated to SQL, so the list is fetched first and sorted in memory
        var sortedResults = results
            .ToList()
            .OrderBy(need => GetPriorityOrder(need.Priority))
            .ToList();

        ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();
        return View(sortedResults);
    }
    
    private int GetPriorityOrder(NeedPriorityEnum priority)
    {
        return priority switch
        {
            NeedPriorityEnum.Critical => 1,
            NeedPriorityEnum.Urgent => 2,
            NeedPriorityEnum.Planned => 3,
            _ => 4
        };
    }
    [HttpGet]
    

    
    [HttpPost]
    public IActionResult RegisterNeed(ResourceNeed need)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            LoadResourceMarkers();
            return View("DashboardIndex", need);
        }
        // Save to database
        need.PublicActorId = DemoDefaults.PublicActorId;
        need.RegisteredAt = DateTime.Now;
        if (need.Id > 0)
        {
            var existingNeed = _db.Needs.Find(need.Id);
            if (existingNeed != null)
            {
                existingNeed.Title = need.Title;
                existingNeed.ContactInfo = need.ContactInfo;
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
                existingNeed.Status = need.Status; 
                _db.SaveChanges();
            }
        }
        else
        {
            need.PublicActorId = DemoDefaults.PublicActorId;
            need.RegisteredAt = DateTime.Now;
            _db.Needs.Add(need);
            _db.SaveChanges();
        }
        _db.SaveChanges();
        // Show the resources near the new need
        return RedirectToAction("NeedsIndex");
    }

    public IActionResult MatchesIndex()
    {
        var needs = _db.Needs.Include(need => need.CategoryInfo).ToList();
        ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();
        return View("NeedsIndex", needs);
    }
    
    // Shows the resources within radiusKm of a need, nearest first.
    public IActionResult Matches(int? needId, double radiusKm = 50)
    {
        ResourceNeed need = null;
        
        if (needId.HasValue)
        {
            need = _db.Needs.Find(needId.Value);
        }
        // If theres no need redirect to dashboard
        else
        {
            need = _db.Needs.OrderByDescending(n => n.Id).FirstOrDefault();
            
        }

        if (need == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid || !double.IsFinite(radiusKm) || radiusKm <= 0)
        {
            radiusKm = 50;
        }
        
        var model = new MatchViewModel
        {
            Need = need,
            RadiusKm = radiusKm,
            UserNeeds = _db.Needs.ToList(),
            Matches = RadiusMatcher.FindResources(
                need, radiusKm, 
                _db.Resources.Include(r => r.Category).ToList())
        };
        // Add the saved decisions from the ResourceMatch table
        var records = _db.MatchRecords.Where(m => m.NeedId == need.Id).ToList();
        foreach (var match in model.Matches)
        {
            var record = records.FirstOrDefault(r => r.ResourceId == match.Resource.Id);
            match.MatchStatus = record?.Status;
            match.Comment = record?.Comment;
        }

        return View("MatchesIndex", model);
    }
    
    public IActionResult ShowAllResourcesInMatches(int needId, double radiusKm = 50)
    {
        var need = _db.Needs.Find(needId);
        if (need == null)
        {
            return NotFound();
        }
        if (!double.IsFinite(radiusKm) || radiusKm <= 0)
        {
            radiusKm = 50;
        }
        var model = new MatchViewModel
        {
            Need = need,
            RadiusKm = radiusKm,
            AllCategories = true,
            UserNeeds = _db.Needs.ToList(),
            Matches = RadiusMatcher.FindResources(
                need, radiusKm, 
                _db.Resources.Include(r => r.Category).ToList(),
                allCategories: true)
        };
        // Add the saved decisions from the ResourceMatch table
        var records = _db.MatchRecords.Where(m => m.NeedId == need.Id).ToList();
        foreach (var match in model.Matches)
        {
            var record = records.FirstOrDefault(r => r.ResourceId == match.Resource.Id);
            match.MatchStatus = record?.Status;
            match.Comment = record?.Comment;
        }

        return View("MatchesIndex", model);
        
    }

    // Saves the decision about a resource for a need in the ResourceMatch table.
    // newStatus: accepted = assigned, proposed = under review, declined = rejected
    [HttpPost]
    public IActionResult UpdateResourceStatus(int resourceId, int needId, string newStatus, string? statusComment)
    {
        if (newStatus is not ("proposed" or "accepted" or "declined"))
        {
            return BadRequest();
        }

        if (_db.Needs.Find(needId) == null || _db.Resources.Find(resourceId) == null)
        {
            return NotFound();
        }

        var comment = string.IsNullOrWhiteSpace(statusComment) ? null : statusComment.Trim();
        if (comment != null && comment.Length > 500)
        {
            comment = comment.Substring(0, 500);
        }

        var record = _db.MatchRecords.Find(needId, resourceId);
        if (record == null)
        {
            record = new MatchRecord
            {
                NeedId = needId,
                ResourceId = resourceId
            };
            _db.MatchRecords.Add(record);
        }

        record.PublicActorId = DemoDefaults.PublicActorId;
        record.Status = newStatus;
        record.Comment = comment;
        record.MatchDate = DateTime.Now;
        _db.SaveChanges();

        return RedirectToAction("Matches", new { needId });
    }
}



public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Index(int? id)
    {
        ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();

        if (id.HasValue)
        {
            var existingResource = _db.Resources.Find(id.Value);
            if (existingResource != null)
            {
                return View(existingResource);
            }
        }
        
        // Sends an empty model to the form the first time the page is opened.
        return View(new ResourceEntry());     
    }
    
    [HttpPost]
    public async Task<IActionResult> SelectRole(string userType)
    {
        if (userType is not ("ResourceProvider" or "PublicActor"))
        {
            userType = "ResourceProvider";
        }

        var displayName = userType == "PublicActor" ? "Public Actor" : "Resource Provider";
        var claims = User.Claims
            .Where(claim => claim.Type is not (ClaimTypes.Name or ClaimTypes.Role or "UserType"))
            .ToList();
        claims.Add(new Claim(ClaimTypes.Name, displayName));
        claims.Add(new Claim(ClaimTypes.Role, userType));
        claims.Add(new Claim("UserType", userType));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true
            });

        if (userType == "PublicActor")
        {
            // Route user to dashboard for Public Actor 
            return RedirectToAction("DashboardIndex", "PublicActor");
        }

        // Standard fallback: Route to civilian/resource provider page
        return RedirectToAction("Index", "Home");
    }

    

    // Runs when the form is submitted, saves the resource, and redirects to the Resources page.
    [HttpPost]
    public IActionResult RegisterResource(ResourceEntry resource)
    {
        var resourceType = _db.Categories.Find(resource.CategoryId)?.CategoryName ?? "";
        if (resource.ExpirationDate is null && !string.IsNullOrWhiteSpace(resourceType))
        {
            resource.ExpirationDate = GetExpiryDate(resourceType);
        }


        if (!ModelState.IsValid)
        {
            // sends the user to form with attributes and error messages filled out
            ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();
            return View("Index", resource);
        }

        //save and update only when evrything is valid
        if (resource.Id > 0)
        {
            var existingResource = _db.Resources.Find(resource.Id);
            if (existingResource != null)
            {
                existingResource.Name = resource.Name;
                existingResource.Description = resource.Description;
                existingResource.CategoryId = resource.CategoryId;
                existingResource.Latitude = resource.Latitude;
                existingResource.Longitude = resource.Longitude;
                existingResource.Phone = resource.Phone;
                existingResource.ExpirationDate = resource.ExpirationDate;
                existingResource.AvailableFrom = resource.AvailableFrom;
                existingResource.RegisteredAt = DateTime.Now;
            }
        }
        else
        {
            resource.ProviderId = DemoDefaults.ProviderId;
            resource.RegionId = DemoDefaults.RegionId;
            resource.RegisteredAt = DateTime.Now;
            _db.Resources.Add(resource);
        }
        _db.SaveChanges();
        return RedirectToAction("Index", "Resources");
    }

    // Calculates the expiry date based on the resource category.
    private static DateTime? GetExpiryDate(string resourceType)
    {
        return resourceType switch
        {
            "Shelter" or "Transport" or "Other" or
                "Drone" or "ATV" or "Tractor" or "Excavator" or "Truck" or "Boat" or "Crew" or
                "Transport/logistics" or "Evacuation / Shelter" or "Plowing & Snow Clearing" or "Manpower" or
                "Drone observation" or "Evacuation" or "Snow clearing" or "Crew / personnel"
                => DateTime.Today.AddYears(1),
            "Food" => DateTime.Today.AddDays(14),
            "Medical" => DateTime.Today.AddMonths(1),
            "Materials" or "Generator" or "Sand/Gravel" or
                "Heavy machinery" or "Power Supply / Generator" or "Power / generator" or "Sand / gravel"
                => DateTime.Today.AddMonths(6),
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
        var resource = _db.Resources.Find(id);
        if (resource != null)
        {
            _db.Resources.Remove(resource);
            _db.SaveChanges();
        }
        return RedirectToAction("index", "Resources");
    }

    [HttpPost]
    public IActionResult DeleteNeed(int id)
    {
        var need = _db.Needs.Find(id);
        if (need != null)
        {
            _db.Needs.Remove(need);
            _db.SaveChanges();
        }
        return RedirectToAction("NeedsIndex", "PublicActor");
    }
    
    // [Authorize(Roles = "PublicActor")]
    // public IActionResult CreateIncident()
    //{
        // Show form for creating an incident 
    //}
    
    
}
