using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Helpers;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace H_st_2026_Gruppe_1.Controllers;

public class PublicActorController : Controller
{
    private int? GetCurrentOrganizationId() => _db.GetOrganizationId(User.GetUserId());

    private bool BelongsToCurrentOrganization(int publicActorId)
    {
        var organizationId = GetCurrentOrganizationId();

        return organizationId.HasValue &&
            _db.PublicActors.Any(actor =>
                actor.PublicActorId == publicActorId &&
                actor.OrganizationId == organizationId.Value);
    }
    
    private readonly AppDbContext _db;
    private readonly MatchNeedLifecycle _lifecycle;
    private readonly LocationRegionResolver? _locationRegionResolver;

    public PublicActorController(
        AppDbContext db,
        MatchNeedLifecycle lifecycle,
        LocationRegionResolver? locationRegionResolver = null)
    {
        _db = db;
        _lifecycle = lifecycle;
        _locationRegionResolver = locationRegionResolver;
    }
    

   //Fetches the resources from map.
    private IQueryable<ResourceEntry> GetResourceMarkerQuery(
        bool includeUnavailable = false)
    {
        IQueryable<ResourceEntry> query = _db.Resources
            .AsNoTracking()
            .Where(r =>
                r.Latitude.HasValue &&
                r.Longitude.HasValue);

    if (includeUnavailable)
    {
        // If the user activates filtering on the map,
        // we will also include unavailable resources.
        query = query.Where(r =>
            r.Status == "available" ||
            r.Status == "unavailable");
    }
    else
        {
           //Standard view: Only availabke resources
           query = query.Where(r =>
                r.Status == "available");
        }
        return query;
    }
    private List<object> GetResourceMarkerData(bool includeUnavailable)
    {
        return GetResourceMarkerQuery(includeUnavailable)
            .Select(r => new
            {
                id = r.Id,
                name = r.Name,
                resourceType = r.Category != null
                    ? r.Category.CategoryName
                    : "Annet",
                description = r.Description,
                phone = r.PhoneNumber,
                latitude = r.Latitude,
                longitude = r.Longitude,
                status = r.Status
            })
            .ToList<object>();
    }

    private void LoadResourceMarkers()
    {
        //Makes the resources available for JavaScript on the map
        ViewBag.ResourceMarkersJson =
            System.Text.Json.JsonSerializer.Serialize(GetResourceMarkerData(false));
    }

    // API-endpoint like JavaScript is used to fetch fresh data.
    //includeUnavailable is set to false as a standard
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult GetResourceMarkers(bool includeUnavailable = false)
    {
        return Json(GetResourceMarkerData(includeUnavailable));
    }

    // Categories for the dropdown
    private void LoadDropdowns()
    {
        var categories = _db.Categories
            .OrderBy(category => category.CategoryId)
            .ToList();
        ViewBag.Categories = categories;
        ViewBag.ResourceCategories = categories;
        ViewBag.NeedCategories = _db.Categories
            .Where(category => _db.Needs.Any(need => need.Category == category.CategoryId))
            .OrderBy(category => category.CategoryName)
            .ToList();
        ViewBag.Incidents = _db.Incidents.OrderBy(i => i.IncidentId).ToList();
    }
    
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
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

            if (need.PublicActorId != User.GetUserId())
            {
                return Forbid();
            }

            return View(need);
            
        }

        var currentUser = _db.Users.Find(User.GetUserId());
        return View(new ResourceNeed
        {
            Email = currentUser?.Email ?? "",
            PhoneNumber = currentUser?.PhoneNumber ?? ""
        });
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult RegistrationForm()
    {
        LoadDropdowns();
        var currentUser = _db.Users.Find(User.GetUserId());
        return PartialView(
            "~/Views/PublicActor/_NeedRegistrationForm.cshtml",
            new ResourceNeed
            {
                Email = currentUser?.Email ?? "",
                PhoneNumber = currentUser?.PhoneNumber ?? ""
            });
    }

    private void LoadOperationalFeed()
    {
        var currentUserId = User.GetUserId();

        ViewBag.FeedResources = OperationalFeedHelper.GetResourceItems(
            _db.Resources.Where(resource => resource.Status == "available"));
        ViewBag.FeedNeeds = _db.Needs
            .Where(need => need.Status != "resolved" && need.Status != "cancelled")
            .Include(need => need.CategoryInfo)
            .AsEnumerable()
            .Select(need => new OperationalFeedItem
            {
                Type = "need",
                Id = need.Id,
                CanEdit = need.PublicActorId == currentUserId,
                Title = need.Title,
                Description = need.Description,
                Status = need.Status,
                Category = need.CategoryInfo?.CategoryName,
                LocationName = need.LocationName,
                Priority = need.Priority.ToString(),
                ColorKey = need.Priority == NeedPriorityEnum.Critical
                    ? "critical-need"
                    : "planned-need",
                Latitude = need.Latitude,
                Longitude = need.Longitude,
                CreatedAt = need.RegisteredAt
            })
            .ToList();
        ViewBag.FeedIncidents = OperationalFeedHelper.GetIncidentItems(_db, GetCurrentOrganizationId());
    }
    
    
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult NeedsIndex(
        NeedPriorityEnum? priority,
        int? category,
        DateTime? startDate,
        DateTime? endDate,
        int? id)
    {
        _lifecycle.ReleaseFinishedAssignments();
        
        var results = _db.Needs.Include(need => need.CategoryInfo).AsQueryable();

        var organizationId = GetCurrentOrganizationId();

        if (organizationId is null)
        {
            throw new InvalidOperationException(
                "The authenticated PublicActor is missing an organization.");
        }

        results = results.Where(need =>
            need.Status != "resolved" && need.Status != "cancelled" &&
            _db.PublicActors.Any(actor =>
                actor.PublicActorId == need.PublicActorId &&
                actor.OrganizationId == organizationId.Value));

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

        var needIds = sortedResults.Select(need => need.Id).ToList();
        ViewBag.NeedMatchStatuses = (from match in _db.MatchRecords
            join resource in _db.Resources on match.ResourceId equals resource.Id
            where needIds.Contains(match.NeedId)
                && (match.Status == "proposed" || match.Status == "accepted")
            select new NeedMatchStatusViewModel
            {
                NeedId = match.NeedId,
                ResourceName = resource.Name,
                LocationName = resource.LocationName,
                Status = match.Status
            }).ToList();

        LoadDropdowns();
        if (id.HasValue)
        {
            var editNeed = sortedResults.FirstOrDefault(need => need.Id == id.Value);
            if (editNeed == null)
            {
                return NotFound();
            }

            if (editNeed.PublicActorId != User.GetUserId())
            {
                return Forbid();
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

    private bool CanRegisterNeed() => User.IsInRole("PublicActor");


    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public IActionResult RegisterNeed(ResourceNeed need)
    {
        if (!CanRegisterNeed())
        {
            return RedirectToAction("Index", "Home");
        }

        var currentUserId = User.GetUserId();
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            LoadResourceMarkers();
            LoadOperationalFeed();
            ViewBag.OpenRegistrationModal = true;
            return View("DashboardIndex", need);
        }

        var location = _locationRegionResolver is null
            ? LocationRegionResult.Valid(DemoDefaults.RegionId)
            : _locationRegionResolver.Resolve(need.Latitude, need.Longitude);
        if (!location.IsValid)
        {
            ModelState.AddModelError(nameof(ResourceNeed.Latitude), location.ErrorMessage!);
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

            if (existingNeed.PublicActorId != User.GetUserId())
            {
                return Forbid();
            }
            
            existingNeed.Title = need.Title;
            existingNeed.Email = need.Email;
            existingNeed.PhoneNumber = need.PhoneNumber;
            existingNeed.LocationName = need.LocationName;
            existingNeed.CrisisEventId = need.CrisisEventId;
            existingNeed.Latitude = need.Latitude;
            existingNeed.Longitude = need.Longitude;
            existingNeed.RegionId = location.RegionId!.Value;
            existingNeed.Category = need.Category;
            existingNeed.Description = need.Description;
            existingNeed.Quantity = need.Quantity;
            existingNeed.RequiredFrom = need.RequiredFrom;
            existingNeed.Deadline = need.Deadline;
            existingNeed.Priority = need.Priority;
            existingNeed.RegisteredAt = DateTime.Now;
            _db.SaveChanges();
        }
        else
        {
            if (!User.IsInRole("PublicActor"))
            {
                return Forbid();
            }

            need.Status = "new";
            need.PublicActorId = currentUserId.Value;
            need.RegionId = location.RegionId!.Value;
            need.RegisteredAt = DateTime.Now;
            _db.Needs.Add(need);
            _db.SaveChanges();
        }
        // Show the resources near the new need
        return RedirectToAction("NeedsIndex");
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult MatchesIndex(int? needId, double radiusKm = 0)
    {
        return View("MatchesIndex", BuildMatchViewModel(needId, radiusKm));
    }
    
    // Shows the resources within radiusKm of a need, nearest first.
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult Matches(int? needId, double radiusKm = 0)
    {
        var model = BuildMatchViewModel(needId, radiusKm);
        if (model.Need.Id == 0)
        {
            return RedirectToAction(nameof(MatchesIndex));
        }

        return View("MatchesIndex", model);
    }
    
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
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

        var organizationId = GetCurrentOrganizationId();

        if (organizationId is null)
        {
            throw new InvalidOperationException(
                "The authenticated PublicActor is missing an organization.");
        }

        var needs = _db.Needs
            .Include(need => need.CategoryInfo)
            .Where(need => need.Status != "resolved" && need.Status != "cancelled" &&
                _db.PublicActors.Any(actor =>
                    actor.PublicActorId == need.PublicActorId &&
                    actor.OrganizationId == organizationId.Value))
            .ToList();
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

        // Ressurser som allerede er tildelt dette behovet skal fortsatt vises,
        // ellers forsvinner knappene og tildelingen kan ikke endres.
        var assignedResourceIds = records
            .Where(match => match.Status == "accepted")
            .Select(match => match.ResourceId)
            .ToList();

        // Utløpte ressurser vises ikke. Sluttdato uten klokkeslett gjelder ut dagen.
        var now = DateTime.Now;
        var today = DateTime.Today;

        model.Matches = RadiusMatcher.FindResources(
            selectedNeed,
            radiusKm,
            _db.Resources
                .Include(resource => resource.Category)
                .Where(resource => resource.Status == "available"
                        && (resource.ExpirationDate == null
                            || resource.ExpirationDate >= now
                            || resource.ExpirationDate == today)
                    || assignedResourceIds.Contains(resource.Id))
                .ToList(),
            allCategories);
        
        // Fetch earlier match-status' and comments for resources
        // that are still available.
        foreach (var match in model.Matches)
        {
            var record = records.FirstOrDefault(
                item => item.ResourceId == match.Resource.Id);

            match.MatchStatus = record?.Status;
            match.Comment = record?.Comment;
        }
 
        return model;
    }

    // Saves the decision about a resource for a need in the ResourceMatch table.
    // newStatus: proposed = request sent to the provider, declined = withdrawn.
    // The provider sets accepted/declined in ResourcesController.RespondToMatch.
    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateResourceStatus(int resourceId, int needId, string newStatus, string? statusComment)
    {
        if (newStatus is not ("proposed" or "declined"))
        {
            return BadRequest();
        }

        var need = _db.Needs.Find(needId);
        if (need == null || !_db.Resources.Any(resource => resource.Id == resourceId))
        {
            return NotFound();
        }

        if (!BelongsToCurrentOrganization(need.PublicActorId))
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

        if (need.Status is "resolved" or "cancelled"
            || need.Deadline.HasValue && need.Deadline.Value < DateTime.Now)
        {
            TempData["MatchError"] = "Dette behovet er lukket eller fristen har gått ut. Statusen kan ikke lenger endres.";
            return RedirectToAction("Matches", new { needId });
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
            "proposed" => "Forslaget er sendt til ressursleverandøren.",
            "declined" when wasAssigned => "Forslaget er trukket tilbake og ressursen er tilgjengelig igjen.",
            "declined" => "Forslaget er trukket tilbake.",
            _ => "Status er lagret."
        };

        return RedirectToAction("Matches", new { needId });
    }

    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public IActionResult ResolveNeed(int id)
    {
        var need = _db.Needs.Find(id);
        if (need == null)
        {
            return NotFound();
        }

        if (!BelongsToCurrentOrganization(need.PublicActorId))
        {
            return Forbid();
        }

        _lifecycle.CloseNeed(id, "resolved");
        return RedirectToAction(nameof(NeedsIndex));
    }

    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNeed(int id)
    {
        var need = await _db.Needs.FindAsync(id);
        if (need == null)
        {
            return NotFound();
        }

        if (need.PublicActorId != User.GetUserId())
        {
            return Forbid();
        }

        // beholder historikken
        _lifecycle.CloseNeed(need.Id);

        need.Status = "cancelled";
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(NeedsIndex));
    }
}
