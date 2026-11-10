using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace H_st_2026_Gruppe_1.Controllers;

public class IncidentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly LocationRegionResolver? _locationRegionResolver;

    public IncidentsController(
        AppDbContext db,
        LocationRegionResolver? locationRegionResolver = null)
    {
        _db = db;
        _locationRegionResolver = locationRegionResolver;
    }

    private int? GetCurrentOrganizationId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var currentUserId))
        {
            return null;
        }

        return _db.PublicActors
            .Where(actor => actor.PublicActorId == currentUserId)
            .Select(actor => (int?)actor.OrganizationId)
            .FirstOrDefault();
    }

    private bool BelongsToCurrentOrganization(int publicActorId)
    {
        var organizationId = GetCurrentOrganizationId();

        return organizationId.HasValue &&
            _db.PublicActors.Any(actor =>
                actor.PublicActorId == publicActorId &&
                actor.OrganizationId == organizationId.Value);
    }
    
    public IActionResult Index(
        string? search,
        string? status,
        string? incidentType,
        DateTime? startDate,
        DateTime? endDate,
        int? id = null)
    {
        var incidents = GetIncidents(search, status, incidentType, startDate, endDate);
        var currentOrganizationId = GetCurrentOrganizationId();
        var editableIncidentOwnerIds = currentOrganizationId.HasValue
            ? _db.PublicActors
                .Where(actor => actor.OrganizationId == currentOrganizationId.Value)
                .Select(actor => actor.PublicActorId)
                .ToHashSet()
            : new HashSet<int>();

        if (id.HasValue)
        {
            var incident = incidents.FirstOrDefault(item => item.IncidentId == id.Value);
            if (incident == null ||
                !User.IsInRole("PublicActor") ||
                !BelongsToCurrentOrganization(incident.PublicActorId))
            {
                return NotFound();
            }

            ViewBag.EditIncident = ToEditModel(incident);
        }

        return View(new IncidentIndexViewModel
        {
            Incidents = incidents,
            EditableIncidentOwnerIds = editableIncidentOwnerIds,
            Search = search
        });
    }

    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public IActionResult Create(IncidentIndexViewModel model)
    {
        var createModel = model.CreateModel;
        if (!ModelState.IsValid)
        {
            model.Incidents = GetIncidents();
            return View("Index", model);
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var location = _locationRegionResolver is null
            ? LocationRegionResult.Valid(DemoDefaults.RegionId)
            : _locationRegionResolver.Resolve(
                (double)createModel.Latitude!.Value,
                (double)createModel.Longitude!.Value);
        if (!location.IsValid)
        {
            ModelState.AddModelError(nameof(IncidentCreateViewModel.Latitude), location.ErrorMessage!);
            model.Incidents = GetIncidents();
            return View("Index", model);
        }

        var incident = new Incident
        {
            PublicActorId = currentUserId,
            RegionId = location.RegionId!.Value,
            Title = createModel.Title.Trim(),
            NormalizedTitle = NormalizeTitle(createModel.Title),
            IncidentType = createModel.IncidentType,
            Description = string.IsNullOrWhiteSpace(createModel.Description)
                ? null
                : createModel.Description.Trim(),
            Status = "active",
            Latitude = createModel.Latitude!.Value,
            Longitude = createModel.Longitude!.Value,
            CreatedAt = DateTime.Now
        };

        _db.Incidents.Add(incident);
        _db.SaveChanges();

        TempData["IncidentCreated"] = "Hendelsen ble registrert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(IncidentCreateViewModel model)
    {
        var incident = _db.Incidents.Find(model.IncidentId);
        if (incident == null)
        {
            return NotFound();
        }

        if (!BelongsToCurrentOrganization(incident.PublicActorId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.EditIncident = model;
            return View("Index", new IncidentIndexViewModel
            {
                Incidents = GetIncidents(),
                Search = model.Title
            });
        }

        var location = _locationRegionResolver is null
            ? LocationRegionResult.Valid(DemoDefaults.RegionId)
            : _locationRegionResolver.Resolve(
                (double)model.Latitude!.Value,
                (double)model.Longitude!.Value);
        if (!location.IsValid)
        {
            ModelState.AddModelError(nameof(IncidentCreateViewModel.Latitude), location.ErrorMessage!);
            ViewBag.EditIncident = model;
            return View("Index", new IncidentIndexViewModel
            {
                Incidents = GetIncidents(),
                Search = model.Title
            });
        }

        incident.Title = model.Title.Trim();
        incident.NormalizedTitle = NormalizeTitle(model.Title);
        incident.IncidentType = model.IncidentType;
        incident.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();
        incident.Latitude = model.Latitude!.Value;
        incident.Longitude = model.Longitude!.Value;
        incident.RegionId = location.RegionId!.Value;

        _db.SaveChanges();

        TempData["IncidentUpdated"] = "Hendelsen ble oppdatert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public IActionResult MarkAsResolved(int incidentId)
    {
        var incident = _db.Incidents.Find(incidentId);
        if (incident == null)
        {
            return NotFound();
        }
        if (!BelongsToCurrentOrganization(incident.PublicActorId))
        {
            return Forbid();
        }

        incident.Status = "resolved";
        _db.SaveChanges();

        TempData["IncidentUpdated"] = "Hendelsen er markert som løst.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "PublicActor")]
    [ValidateAntiForgeryToken]
    public IActionResult Reactivate(int incidentId)
    {
        var incident = _db.Incidents.Find(incidentId);
        if (incident == null)
        {
            return NotFound();
        }
        
        if (!BelongsToCurrentOrganization(incident.PublicActorId))
        {
            return Forbid();
        }

        incident.Status = "active";
        _db.SaveChanges();

        TempData["IncidentUpdated"] = "Hendelsen er aktivert på nytt.";
        return RedirectToAction(nameof(Index));
    }

    private List<Incident> GetIncidents(
        string? search = null,
        string? status = null,
        string? incidentType = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var incidents = _db.Incidents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = NormalizeTitle(search);
            incidents = incidents.Where(incident => incident.NormalizedTitle.Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            incidents = incidents.Where(incident => incident.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(incidentType))
        {
            incidents = incidents.Where(incident => incident.IncidentType == incidentType);
        }

        if (startDate.HasValue)
        {
            incidents = incidents.Where(incident => incident.CreatedAt >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            incidents = incidents.Where(incident => incident.CreatedAt < endDate.Value.Date.AddDays(1));
        }

        return incidents
            .OrderBy(incident => incident.Status == "resolved" ? 1 : 0)
            .ThenByDescending(incident => incident.CreatedAt)
            .ToList();
    }

    private static string NormalizeTitle(string title)
    {
        return title.Trim().ToLowerInvariant();
    }

    private static IncidentCreateViewModel ToEditModel(Incident incident)
    {
        return new IncidentCreateViewModel
        {
            IncidentId = incident.IncidentId,
            Title = incident.Title,
            IncidentType = incident.IncidentType,
            Description = incident.Description,
            Latitude = incident.Latitude,
            Longitude = incident.Longitude
        };
    }
}