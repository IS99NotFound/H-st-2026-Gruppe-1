using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using System.Text;
using System.Text.Json;
using System.Globalization;
using H_st_2026_Gruppe_1.Services;
using H_st_2026_Gruppe_1.Helpers;

namespace H_st_2026_Gruppe_1.Controllers;

[Authorize(Roles = "PublicActor, Administrator")]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IReportsService _reportsService;

    public ReportsController(AppDbContext db, IReportsService reportsService)
    {
        _db = db;
        _reportsService = reportsService;
    }

    [Authorize(Roles = "PublicActor, Administrator")]
    public async Task<IActionResult> Index(DateTime? Fromdate, DateTime? Todate, int? Category, string? Location)
    {
        var viewmodel = await _reportsService.GetReportDataAsynch(Fromdate, Todate, Category, Location);
        return View(viewmodel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAllCsv(
        DateTime? Fromdate, DateTime? Todate, int? Category, string? Location)
    {
        var report = await _reportsService.GetReportDataAsynch(Fromdate, Todate, Category, Location);
        static string Csv(string? value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        static string FormatDate(DateTime? value) =>
            value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine("Type;ID;Navn/tittel;Kategori/type;Beskrivelse;Sted;Status;Prioritet;E-post;Telefon;Registrert;Frist;Utløpsdato");

        foreach (var resource in report.Resources)
        {
            builder.AppendLine(string.Join(";",
                Csv("Ressurs"),
                Csv(resource.Id.ToString(CultureInfo.InvariantCulture)),
                Csv(resource.Name),
                Csv(CategoryLabelHelper.DisplayName(resource.Category)),
                Csv(resource.Description),
                Csv(resource.LocationName),
                Csv(StatusLabelHelper.DisplayName(resource.Status)),
                Csv(string.Empty),
                Csv(resource.Email),
                Csv(resource.PhoneNumber),
                Csv(FormatDate(resource.RegisteredAt)),
                Csv(string.Empty),
                Csv(FormatDate(resource.ExpirationDate))));
        }

        foreach (var incident in report.Incidents)
        {
            var location = string.Create(CultureInfo.InvariantCulture,
                $"{incident.Latitude}, {incident.Longitude}");
            builder.AppendLine(string.Join(";",
                Csv("Hendelse"),
                Csv(incident.Id.ToString(CultureInfo.InvariantCulture)),
                Csv(incident.Title),
                Csv(incident.IncidentType),
                Csv(incident.Description),
                Csv(location),
                Csv(StatusLabelHelper.DisplayName(incident.Status)),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(string.Empty),
                Csv(FormatDate(incident.CreatedAt)),
                Csv(string.Empty),
                Csv(string.Empty)));
        }

        foreach (var need in report.Needs)
        {
            builder.AppendLine(string.Join(";",
                Csv("Behov"),
                Csv(need.Id.ToString(CultureInfo.InvariantCulture)),
                Csv(need.Title),
                Csv(need.Category),
                Csv(need.Description),
                Csv(need.LocationName),
                Csv(StatusLabelHelper.DisplayName(need.Status)),
                Csv(need.Priority),
                Csv(need.Email),
                Csv(need.PhoneNumber),
                Csv(FormatDate(need.RegisteredAt)),
                Csv(FormatDate(need.Deadline)),
                Csv(string.Empty)));
        }

        var fileName = $"rapport-{DateTime.Now:yyyyMMdd}.csv";
        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        return File(content, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor, Administrator")]
    public IActionResult ResourcesView()
    {
        return View(GetResourceReportRows());
    }

    [HttpGet]
    [Authorize(Roles = "PublicActor, Administrator")]
    public async Task<IActionResult> ExportunansweredNeedsCsv
    (DateTime? Fromdate, DateTime? Todate, int? Category,
        string? Location)
    {
        var needs = await _reportsService.GetUnansweredNeedsAsync(Fromdate, Todate, Category, Location);
        var builder = new StringBuilder();
        builder.AppendLine("Id;Tittel;Kategori;Sted;Prioritet;Registrert;Frist");
        foreach (var n in needs)
        {
            builder.AppendLine($"{n.Id};\"{n.Title}\";\"{n.Category}" +
                               $"\";\"{n.LocationName}\";" +
                               $"\"{n.Priority}\";{n.RegisteredAt:yyyy-MM-dd HH:mm}" +
                               $";{n.Deadline:yyyy-MM-dd HH:mm}");
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(),
            "text/csv; charset=utf-8", $"ubesvarte-behov-{DateTime.Now:yyyyMMdd}.csv");
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
            "text/csv; charset=utf-8", "ressurser.csv");
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

        return File(Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8", "ressurser.json");
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

}