using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Controllers;

[AllowAnonymous]
public class GuestController : Controller
{
    private static readonly IReadOnlyDictionary<string, (double Latitude, double Longitude)> RegionCenters =
        new Dictionary<string, (double Latitude, double Longitude)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Agder"] = (58.35, 7.95),
            ["Innlandet"] = (61.05, 10.35),
            ["Møre og Romsdal"] = (62.65, 7.75),
            ["Nordland"] = (66.35, 14.55),
            ["Oslo"] = (59.91, 10.75),
            ["Rogaland"] = (59.1, 5.75),
            ["Troms"] = (69.4, 19.0),
            ["Trøndelag"] = (63.4, 10.4),
            ["Vestfold"] = (59.3, 10.25),
            ["Vestland"] = (60.75, 6.3),
            ["Østfold"] = (59.35, 11.4),
            ["Svalbard"] = (78.2, 15.6)
        };

    private readonly AppDbContext _db;

    public GuestController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        var incidents = _db.Incidents
            .AsNoTracking()
            .Where(incident => incident.Status == "active")
            .Join(
                _db.Regions,
                incident => incident.RegionId,
                region => region.RegionId,
                (incident, region) => new
                {
                    IncidentType = incident.IncidentType,
                    RegionName = region.RegionName,
                    incident.CreatedAt
                })
            .OrderBy(incident => incident.RegionName)
            .ThenBy(incident => incident.IncidentType)
            .Take(100)
            .ToList();

        var feedIncidents = incidents
            .Where(incident => RegionCenters.ContainsKey(incident.RegionName))
            .Select((incident, index) =>
            {
                var center = RegionCenters[incident.RegionName];
                return new OperationalFeedItem
                {
                    Type = "incident",
                    Id = index + 1,
                    Title = incident.IncidentType + " i " + incident.RegionName,
                    Description = null,
                    Status = "active",
                    Category = incident.IncidentType,
                    LocationName = incident.RegionName,
                    ColorKey = "public-team",
                    Latitude = center.Latitude,
                    Longitude = center.Longitude,
                    CreatedAt = incident.CreatedAt
                };
            })
            .ToList();

        ViewBag.FeedIncidents = feedIncidents;
        return View();
    }
}
