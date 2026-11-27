using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Controllers;

public class ReportsController : Controller
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Index(int? regionId = null)
    {
        ViewBag.Regions = await _db.Regions.AsNoTracking().OrderBy(r => r.RegionName).ToListAsync();
        ViewBag.RegionId = regionId;
        var model =  await GetReportDataAsync(regionId);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult ResourcesView(int? regionId = null)
    {
        return View(GetResourceReportRows(regionId));
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult Resources(int? regionId = null)
    {
        var resources = GetResourceReportRows(regionId);

        static string Csv(string? value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        var builder = new StringBuilder();
        builder.AppendLine("Navn;Kategori;Beskrivelse;Region;Sted;E-post;Telefon;Status;Registrert");
        foreach (var resource in resources)
        {
            builder.AppendLine(string.Join(";",
                Csv(resource.Name),
                Csv(resource.Category),
                Csv(resource.Description),
                Csv(resource.RegionName),
                Csv(resource.LocationName),
                Csv(resource.Email),
                Csv(resource.PhoneNumber),
                Csv(resource.Status),
                Csv(resource.RegisteredAt.ToString("yyyy-MM-dd HH:mm"))));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(),
            "text/csv; charset=utf-8", "Ressurser.csv");
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult ResourcesJson(int? regionId = null)
    {
        var resources = GetResourceReportRows(regionId);
        var json = JsonSerializer.Serialize(resources, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", "Ressurser.json");
    }

    private List<ResourceReportRow> GetResourceReportRows(int? regionId = null)
    {
        return _db.Resources
            .Where(resource => regionId == null || resource.RegionId == regionId)
            .OrderBy(resource => resource.Name)
            .Select(resource => new ResourceReportRow(
                resource.Name,
                resource.Category!.CategoryName,
                resource.Description,
                _db.Regions.Where(region => region.RegionId == resource.RegionId)
                    .Select(region => region.RegionName).FirstOrDefault() ?? "",
                resource.LocationName,
                resource.Email,
                resource.PhoneNumber,
                resource.Status,
                resource.RegisteredAt))
            .ToList();
    }
    
    
    
    
    
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult NeedsView(int? regionId = null)
    {
        return View(GetNeedsReportRows(regionId));
    }
    
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult Needs(int? regionId = null)
    {
        var needs = GetNeedsReportRows(regionId);

        static string Csv(string? value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        var builder = new StringBuilder();
        builder.AppendLine("Tittel;Kategori;Beskrivelse;Region;Sted;Prioritet;E-post;Telefon;Status;Registrert");
        foreach (var need in  needs)
        {
            builder.AppendLine(string.Join(";",
                Csv(need.Title),
                Csv(need.CategoryInfo),
                Csv(need.Description),
                Csv(need.RegionName),
                Csv(need.LocationName), 
                Csv(need.Priority.ToString()),
                Csv(need.Email),
                Csv(need.PhoneNumber),
                Csv(need.Status), 
                Csv(need.RegisteredAt.ToString("yyyy-MM-dd HH:mm"))));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(),
            "text/csv; charset=utf-8", "Behov.csv");
    } 
    
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult NeedsJson(int? regionId = null)
    {
        var needs = GetNeedsReportRows(regionId);
        var json = JsonSerializer.Serialize(needs, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", "Behov.json");
    }
    
    private List<NeedReportViewModel> GetNeedsReportRows(int? regionId = null)
    {
        return _db.Needs
            .Where(Need => regionId == null || Need.RegionId == regionId)
            .OrderBy(Need => Need.Title)
            .Select(Need => new NeedReportViewModel(
                Need.Title,
                Need.CategoryInfo == null ? null : Need.CategoryInfo.CategoryName,
                Need.Description,
                _db.Regions.Where(region => region.RegionId == Need.RegionId)
                    .Select(region => region.RegionName).FirstOrDefault() ?? "",
                Need.LocationName,
                Need.Status,
                Need.Priority,
                Need.PhoneNumber,
                Need.Email,
                Need.RegisteredAt))
            .ToList(); 


}

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult IncidentsView(int? regionId = null)
    {
        return View(GetIncidentReportRows(regionId));
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult Incidents(int? regionId = null)
    {
        var incidents = GetIncidentReportRows(regionId);

        static string Csv(string? value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        var builder = new StringBuilder();
        builder.AppendLine("Tittel;Type;Beskrivelse;Region;Breddegrad;Lengdegrad;Status;Registrert");
        foreach (var incident in incidents)
        {
            builder.AppendLine(string.Join(";",
                Csv(incident.Title),
                Csv(incident.IncidentType),
                Csv(incident.Description),
                Csv(incident.RegionName),
                Csv(incident.Latitude.ToString()),
                Csv(incident.Longitude.ToString()),
                Csv(incident.Status),
                Csv(incident.CreatedAt.ToString("yyyy-MM-dd HH:mm"))));
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(),
            "text/csv; charset=utf-8", "Hendelser.csv");
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public IActionResult IncidentsJson(int? regionId = null)
    {
        var incidents = GetIncidentReportRows(regionId);
        var json = JsonSerializer.Serialize(incidents, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", "Hendelser.json");
    }

    private List<IncidentReportRow> GetIncidentReportRows(int? regionId = null)
    {
        return (
            from incident in _db.Incidents
            join region in _db.Regions on incident.RegionId equals region.RegionId
            where regionId == null || incident.RegionId == regionId
            orderby incident.CreatedAt descending
            select new IncidentReportRow(
                incident.Title,
                incident.IncidentType,
                incident.Description,
                region.RegionName,
                incident.Latitude,
                incident.Longitude,
                incident.Status,
                incident.CreatedAt))
            .ToList();
    }

    private async Task<ReportsIndexViewModel> GetReportDataAsync(int? regionId)
    {
        var incidents = _db.Incidents.AsNoTracking().Where(i => regionId == null || i.RegionId == regionId);
        var resources = _db.Resources.AsNoTracking().Where(r => regionId == null || r.RegionId == regionId);
        var needs = _db.Needs.AsNoTracking().Where(n => regionId == null || n.RegionId == regionId);

        var totalIncidents = await incidents.CountAsync();
        var activeIncidents = await incidents.CountAsync(i => i.Status == "active");
        var resolvedIncidents = await incidents.CountAsync(i => i.Status == "resolved");
        var totalResources = await resources.CountAsync();
        var availableResources = await resources.CountAsync(r => r.Status == "available");

        var totalNeeds = await needs.CountAsync();
        var unansweredNeeds = await needs
            .Where(n => n.Status != "resolved" && n.Status != "cancelled")
            .CountAsync();

        return new ReportsIndexViewModel
        {
            TotalIncidents = totalIncidents,
            ActiveIncidents = activeIncidents,
            TotalResources = totalResources,
            AvailableResources = availableResources,
            TotalNeeds = totalNeeds,
            UnansweredNeeds = unansweredNeeds,
            ResolvedIncidents = resolvedIncidents
        };
    }
   
} 