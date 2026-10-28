using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace H_st_2026_Gruppe_1.Controllers;

public class IncidentsController : Controller
{
    private readonly AppDbContext _db;

    public IncidentsController(AppDbContext db)
    {
        _db = db;
    }
    
    public IActionResult Index(
        string? status,
        string? incidentType,
        string? dateFilter,
        DateTime? startDate,
        DateTime? endDate)
    {
        var incidents = GetIncidents(status, incidentType, dateFilter, startDate, endDate);

        return View(new IncidentIndexViewModel
        {
            Incidents = incidents
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

        var incident = new Incident
        {
            PublicActorId = DemoDefaults.PublicActorId,
            Title = createModel.Title.Trim(),
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

        TempData["IncidentCreated"] = "Incident created successfully.";
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

        incident.Status = "resolved";
        _db.SaveChanges();

        TempData["IncidentUpdated"] = "Incident marked as resolved.";
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

        incident.Status = "active";
        _db.SaveChanges();

        TempData["IncidentUpdated"] = "Incident reactivated.";
        return RedirectToAction(nameof(Index));
    }

    private List<Incident> GetIncidents(
        string? status = null,
        string? incidentType = null,
        string? dateFilter = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var incidents = _db.Incidents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            incidents = incidents.Where(incident => incident.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(incidentType))
        {
            incidents = incidents.Where(incident => incident.IncidentType == incidentType);
        }

        if (!string.IsNullOrWhiteSpace(dateFilter))
        {
            var now = DateTime.Now;
            var fromDate = dateFilter switch
            {
                "1day" => now.AddDays(-1),
                "7days" => now.AddDays(-7),
                "30days" => now.AddDays(-30),
                _ => now.AddYears(-100)
            };

            incidents = incidents.Where(incident => incident.CreatedAt >= fromDate);
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
}