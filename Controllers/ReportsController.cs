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

    [Authorize(Roles = "PublicActor, Administrator")]
    public async Task<IActionResult> Index()
    {
        var model =  await GetReportDataAsync();
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult ResourcesView()
    {
        return View(GetResourceReportRows());
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult Resources()
    {
        var resources = GetResourceReportRows();

        static string Csv(string? value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        var builder = new StringBuilder();
        builder.AppendLine("Navn;Kategori;Beskrivelse;Sted;E-post;Telefon;Status;Registrert");
        foreach (var resource in resources)
        {
            builder.AppendLine(string.Join(";",
                Csv(resource.Name),
                Csv(resource.Category),
                Csv(resource.Description),
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
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult ResourcesJson()
    {
        var resources = GetResourceReportRows();
        var json = JsonSerializer.Serialize(resources, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", "Ressurser.json");
    }

    private List<ResourceReportRow> GetResourceReportRows()
    {
        return _db.Resources
            .OrderBy(resource => resource.Name)
            .Select(resource => new ResourceReportRow(
                resource.Name,
                resource.Category!.CategoryName,
                resource.Description,
                resource.LocationName,
                resource.Email,
                resource.PhoneNumber,
                resource.Status,
                resource.RegisteredAt))
            .ToList();
    }
    
    
    
    
    
    [HttpGet]
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult NeedsView()
    {
        return View(GetNeedsReportRows());
    }
    
    [HttpGet]
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult Needs()
    {
        var needs = GetNeedsReportRows();

        static string Csv(string? value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

        var builder = new StringBuilder();
        builder.AppendLine("Tittel;Kategori;Beskrivelse;Sted;Prioritet;E-post;Telefon;Status;Registrert");
        foreach (var need in  needs)
        {
            builder.AppendLine(string.Join(";",
                Csv(need.Title),
                Csv(need.CategoryInfo),
                Csv(need.Description),
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
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult NeedsJson()
    {
        var needs = GetNeedsReportRows();
        var json = JsonSerializer.Serialize(needs, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", "Behov.json");
    }
    
    private List<NeedReportViewModel> GetNeedsReportRows()
    {
        return _db.Needs
            .OrderBy(Need => Need.Title)
            .Select(Need => new NeedReportViewModel(
                Need.Title,
                Need.CategoryInfo == null ? null : Need.CategoryInfo.CategoryName,
                Need.Description,
                Need.LocationName,
                Need.Status,
                Need.Priority,
                Need.PhoneNumber,
                Need.Email,
                Need.RegisteredAt))
            .ToList(); 


}

    [HttpGet]
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult IncidentsView()
    {
        return View(GetIncidentReportRows());
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult Incidents()
    {
        var incidents = GetIncidentReportRows();

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
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult IncidentsJson()
    {
        var incidents = GetIncidentReportRows();
        var json = JsonSerializer.Serialize(incidents, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", "Hendelser.json");
    }

    private List<IncidentReportRow> GetIncidentReportRows()
    {
        return (
            from incident in _db.Incidents
            join region in _db.Regions on incident.RegionId equals region.RegionId
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

    private async Task<ReportsIndexViewModel> GetReportDataAsync()
    {

        var totalIncidents = await _db.Incidents.AsNoTracking().CountAsync();
        var activeIncidents = await _db.Incidents.AsNoTracking().CountAsync(i => i.Status == "active"); 
        var resolvedIncidents = await _db.Incidents.AsNoTracking().CountAsync(i => i.Status == "resolved");
        var totalResources = await _db.Resources.AsNoTracking().CountAsync();
        var availableResources = await _db.Resources.AsNoTracking().CountAsync(r => r.Status == "available");

        var totalNeeds = await _db.Needs.AsNoTracking().CountAsync();
        var unansweredNeeds = await _db.Needs
            .AsNoTracking()
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