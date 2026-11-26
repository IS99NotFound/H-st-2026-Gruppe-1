using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Helpers;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace H_st_2026_Gruppe_1.Controllers;

public class HomeController : Controller

{
    private readonly AppDbContext _db;
    private readonly MatchNeedLifecycle _lifecycle;
    private readonly LocationRegionResolver? _locationRegionResolver;

    public HomeController(
        AppDbContext db,
        MatchNeedLifecycle lifecycle,
        LocationRegionResolver? locationRegionResolver = null)
    {
        _db = db;
        _lifecycle = lifecycle;
        _locationRegionResolver = locationRegionResolver;
    }

    private void LoadResourceDropdowns()
    {
        ViewBag.ResourceCategories = _db.Categories
            .OrderBy(category => category.CategoryId)
            .ToList();
    }

    [HttpGet]
    [Authorize(Roles = "CivilianProvider")]
    public IActionResult Index(int? id)
    {
        if (!CanRegisterResource())
        {
            return RedirectToAction("DashboardIndex", "PublicActor");
        }
        
        LoadResourceDropdowns();
        var currentUserId = User.GetUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        LoadOperationalFeed(currentUserId.Value);

        if (id.HasValue)
        {
            var existingResource = _db.Resources.Find(id.Value);
            if (existingResource != null)
            {
                if (existingResource.ProviderId != currentUserId.Value)
                {
                    return Forbid();
                }
            }
            return View(existingResource);

        }
        
        var currentUser = _db.Users.Find(currentUserId.Value);
        return View(new ResourceEntry
        {
            Email = currentUser?.Email ?? "",
            PhoneNumber = currentUser?.PhoneNumber ?? ""
        });
    }

    [HttpGet]
    [Authorize(Roles = "CivilianProvider")]
    public IActionResult RegistrationForm()
    {
        LoadResourceDropdowns();

        var currentUser = _db.Users.Find(User.GetUserId());
        return PartialView(
            "~/Views/Home/_ResourceRegistrationForm.cshtml",
            new ResourceEntry
            {
                Email = currentUser?.Email ?? "",
                PhoneNumber = currentUser?.PhoneNumber ?? ""
            });
    }

    private void LoadOperationalFeed(int currentUserId)
    {
        ViewBag.FeedResources = OperationalFeedHelper.GetResourceItems(
            _db.Resources.Where(resource => resource.ProviderId == currentUserId));
        ViewBag.FeedNeeds = new List<OperationalFeedItem>();
        ViewBag.FeedIncidents = OperationalFeedHelper.GetIncidentItems(
            _db, _db.GetOrganizationId(currentUserId));
    }
    
    
    // Runs when the form is submitted, saves the resource, and redirects to the Resources page.
    [HttpPost]
    [Authorize(Roles = "CivilianProvider")]  
    [ValidateAntiForgeryToken] 
    public IActionResult RegisterResource(ResourceEntry resource)
    {
        if (!CanRegisterResource())
        {
            return RedirectToAction(nameof(Index));
        }
        
        var currentUserId = User.GetUserId();                 
        if (currentUserId is null)             
        {                                                       
            return Forbid();                                    
        }                                                       

        if (!ModelState.IsValid)
        {
            // sends the user to form with attributes and error messages filled out
            LoadResourceDropdowns();
            LoadOperationalFeed(currentUserId.Value);
            ViewBag.OpenRegistrationModal = true;
            return View("Index", resource);
        }

        if (!_db.Categories.Any(category => category.CategoryId == resource.CategoryId))
        {
            ModelState.AddModelError(nameof(ResourceEntry.CategoryId),
                "Velg en gyldig ressurskategori.");
            LoadResourceDropdowns();
            LoadOperationalFeed(currentUserId.Value);
            ViewBag.OpenRegistrationModal = true;
            return View("Index", resource);
        }

        var location = _locationRegionResolver is null
            ? LocationRegionResult.Valid(DemoDefaults.RegionId)
            : _locationRegionResolver.Resolve(resource.Latitude!.Value, resource.Longitude!.Value);
        if (!location.IsValid)
        {
            ModelState.AddModelError(nameof(ResourceEntry.Latitude), location.ErrorMessage!);
            LoadResourceDropdowns();
            LoadOperationalFeed(currentUserId.Value);
            ViewBag.OpenRegistrationModal = true;
            return View("Index", resource);
        }
        
        //save and update only when evrything is valid
        if (resource.Id > 0)
        {
            var existingResource = _db.Resources.Find(resource.Id);
            if (existingResource != null)
            {
                if (existingResource.ProviderId != currentUserId.Value)
                {
                    return Forbid();
                }
                
                existingResource.Name = resource.Name;
                existingResource.Description = resource.Description;
                existingResource.CategoryId = resource.CategoryId;
                existingResource.Latitude = resource.Latitude;
                existingResource.Longitude = resource.Longitude;
                existingResource.RegionId = location.RegionId!.Value;
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
            resource.Status = "available";
            resource.ProviderId = currentUserId.Value;
            resource.RegionId = location.RegionId!.Value;
            resource.RegisteredAt = DateTime.Now;
            _db.Resources.Add(resource);
        }
        _db.SaveChanges();
        return RedirectToAction("Index", "Resources");
    }

    private bool CanRegisterResource()
    {
        return User.IsInRole("CivilianProvider");
    }
    

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [Authorize(Roles = "CivilianProvider")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResource(int id)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }
        
        var currentUserId = User.GetUserId();

        if (currentUserId is null)
        {
            return Unauthorized();
        }
        
        if (resource.ProviderId != currentUserId)
        {
            return Forbid();
        }
        
        resource.Status = "withdrawn";
        await _db.SaveChangesAsync();
        _lifecycle.WithdrawMatches(resource.Id);
        TempData["SuccessMessage"] = "Ressursen er trukket tilbake. Den er ikke lenger tilgjengelig, og matchhistorikken er bevart.";
        return RedirectToAction("Index", "Resources");
    }
}
