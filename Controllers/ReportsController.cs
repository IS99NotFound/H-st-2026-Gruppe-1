using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using H_st_2026_Gruppe_1.Data;
using System.Text;

namespace H_st_2026_Gruppe_1.Controllers;

// Placeholder controller for the reports page. It only shows the view for now.
public class ReportsController : Controller
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize(Roles = "PublicActor")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Resources()
    {
        var resources = _db.Resources
            .OrderBy(resource => resource.Name)
            .Select(resource => new
            {
                resource.Name,
                Category = resource.Category!.CategoryName,
                resource.Description,
                resource.LocationName,
                resource.Email,
                resource.PhoneNumber,
                resource.Status,
                resource.RegisteredAt
            })
            .ToList();

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
}