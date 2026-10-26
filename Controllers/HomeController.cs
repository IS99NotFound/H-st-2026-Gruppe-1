using System.Diagnostics;
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

    public PublicActorController(AppDbContext db)
    {
        _db = db;
    }

    // Categories for the dropdown
    private void LoadDropdowns()
    {
        ViewBag.Categories = _db.Categories.OrderBy(c => c.CategoryId).ToList();
    }

    public IActionResult DashboardIndex()
    {
        LoadDropdowns();
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
    
    
    [HttpPost]
    public IActionResult RegisterNeed(ResourceNeed need)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            return View("DashboardIndex", need);
        }
        

        // Save to database
        need.PublicActorId = DemoDefaults.PublicActorId;
        need.RegisteredAt = DateTime.Now;
        if (need.Id == 0)
        {
            _db.Needs.Add(need);
        }
        else
        {
            _db.Needs.Update(need);
        }
        _db.SaveChanges();

        // Show the resources near the new need
        return RedirectToAction(nameof(Matches), new { needId = need.Id });
    }

    public IActionResult MatchesIndex()
    {
        var needs = _db.Needs.Include(need => need.CategoryInfo).ToList();
        return View("NeedsIndex", needs);
    }
    
    // Shows the resources within radiusKm of a need, nearest first.
    public IActionResult Matches(int? needId, double radiusKm = 50)
    {
        var need = needId.HasValue
            ? _db.Needs.Find(needId.Value)
            : _db.Needs.OrderByDescending(n => n.RegisteredAt).FirstOrDefault();

        // If theres no need redirect to dashboard
        if (need == null)
        {
            return RedirectToAction("DashboardIndex");
        }

        if (!ModelState.IsValid || !double.IsFinite(radiusKm) || radiusKm <= 0)
        {
            radiusKm = 50;
        }

        var model = new MatchViewModel
        {
            Need = need,
            RadiusKm = radiusKm,
            Matches = RadiusMatcher.FindResources(need, radiusKm, _db.Resources.Include(r => r.Category).ToList())
        };
        return View("MatchesIndex", model);
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
}
