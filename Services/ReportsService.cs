using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Helpers;
using H_st_2026_Gruppe_1.Models;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Services;

public interface IReportsService
{
    Task<ReportsIndexViewModel> GetReportDataAsynch(
        DateTime? fromDate, DateTime? toDate, int? category, string? location);

    Task<List<ReportsIndexViewModel.UnansweredNeedRowViewModel>> GetUnansweredNeedsAsync(
        DateTime? fromDate, DateTime? toDate, int? category, string? location);
}

public class ReportsService : IReportsService
{
    private readonly AppDbContext _dbContext;

    public ReportsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportsIndexViewModel> GetReportDataAsynch(
        DateTime? fromDate, DateTime? toDate, int? category, string? location)
    {
        // AsNoTracking passer for rapporter: dataene skal bare leses, ikke endres.
        var incidentsQuery = _dbContext.Incidents.AsNoTracking().AsQueryable();

        var needsQuery = ApplyNeedFilters(
            _dbContext.Needs.AsNoTracking().AsQueryable(),
            fromDate,
            toDate,
            category,
            location);
        var resourcesQuery = _dbContext.Resources.AsNoTracking().AsQueryable();

        // Filtrer fra starten av valgt dato, slik at tidspunkt senere den dagen tas med.
        if (fromDate.HasValue)
        {
            var startDate = fromDate.Value.Date;
            incidentsQuery = incidentsQuery.Where(incident => incident.CreatedAt >= startDate);
            resourcesQuery = resourcesQuery.Where(resource => resource.RegisteredAt >= startDate);
        }

        // Sluttdatoen gjøres eksklusiv ved å gå til neste dag.
        if (toDate.HasValue)
        {
            var endDateExclusive = toDate.Value.Date.AddDays(1);
            incidentsQuery = incidentsQuery.Where(incident => incident.CreatedAt < endDateExclusive);
            resourcesQuery = resourcesQuery.Where(resource => resource.RegisteredAt < endDateExclusive);
        }

        // Kategori og sted begrenser ressursene i rapporten når filterverdier er valgt.
        if (category.HasValue)
        {
            resourcesQuery = resourcesQuery.Where(resource => resource.CategoryId == category.Value);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            resourcesQuery = resourcesQuery.Where(resource => resource.LocationName.Contains(location));
        }

        // CountAsync sender opptellingene til databasen uten å laste alle radene inn i minnet.
        var totalIncidents = await incidentsQuery.CountAsync();
        var activeIncidents = await incidentsQuery.CountAsync(incident => incident.Status == "active");
        var resolvedIncidents = await incidentsQuery.CountAsync(incident => incident.Status == "resolved");

        var totalResources = await resourcesQuery.CountAsync();
        var availableResources = await resourcesQuery.CountAsync(resource => resource.Status == "available");
        var resourcesInUse = await resourcesQuery.CountAsync(resource => resource.Status == "unavailable");

        // En behovsrad regnes som besvart hvis den har en accepted- eller completed-match.
        var unansweredNeeds = await GetUnansweredNeedsAsync(fromDate, toDate, category, location);

        var resources = await resourcesQuery
            .OrderBy(resource => resource.Name)
            .Select(resource => new ReportsIndexViewModel.ReportResourceRowViewModel
            {
                Id = resource.Id,
                Name = resource.Name,
                Category = resource.Category!.CategoryName,
                Description = resource.Description,
                LocationName = resource.LocationName,
                Email = resource.Email,
                PhoneNumber = resource.PhoneNumber,
                Status = resource.Status,
                RegisteredAt = resource.RegisteredAt,
                ExpirationDate = resource.ExpirationDate
            })
            .ToListAsync();

        var incidents = await incidentsQuery
            .OrderByDescending(incident => incident.CreatedAt)
            .Select(incident => new ReportsIndexViewModel.ReportIncidentRowViewModel
            {
                Id = incident.IncidentId,
                Title = incident.Title,
                IncidentType = incident.IncidentType,
                Description = incident.Description,
                Status = incident.Status,
                Latitude = incident.Latitude,
                Longitude = incident.Longitude,
                CreatedAt = incident.CreatedAt
            })
            .ToListAsync();

        var needs = await needsQuery
            .OrderByDescending(need => need.RegisteredAt)
            .Select(need => new
            {
                Need = need,
                CategoryName = need.CategoryInfo!.CategoryName
            })
            .ToListAsync();

        var categoryOptions = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(categoryEntry => categoryEntry.CategoryName)
            .Select(categoryEntry => new ReportsIndexViewModel.CategoryOptionViewModel
            {
                Id = categoryEntry.CategoryId,
                Name = categoryEntry.CategoryName
            })
            .ToListAsync();

        // Samle resultatene i modellen som Reports-siden bruker.
        return new ReportsIndexViewModel
        {
            TotalIncidents = totalIncidents,
            ActiveIncidents = activeIncidents,
            ResolvedIncidents = resolvedIncidents,
            TotalNeeds = await needsQuery.CountAsync(),
            UnansweredNeedsCount = unansweredNeeds.Count,
            TotalResources = totalResources,
            AvailableResources = availableResources,
            ResourcesInUse = resourcesInUse,
            Resources = resources,
            Incidents = incidents,
            Needs = needs.Select(entry => new ReportsIndexViewModel.ReportNeedRowViewModel
            {
                Id = entry.Need.Id,
                Title = entry.Need.Title,
                Category = CategoryLabelHelper.DisplayName(entry.CategoryName),
                Description = entry.Need.Description,
                LocationName = entry.Need.LocationName,
                Email = entry.Need.Email,
                PhoneNumber = entry.Need.PhoneNumber,
                Status = entry.Need.Status,
                Priority = entry.Need.Priority.ToString(),
                RegisteredAt = entry.Need.RegisteredAt,
                Deadline = entry.Need.Deadline
            }).ToList(),
            UnansweredNeeds = unansweredNeeds,
            Categories = categoryOptions,
            FromDate = fromDate,
            ToDate = toDate,
            Category = category,
            Location = location
        };
    }

    public async Task<List<ReportsIndexViewModel.UnansweredNeedRowViewModel>> GetUnansweredNeedsAsync(
        DateTime? fromDate, DateTime? toDate, int? category, string? location)
    {
        // Finn behov som ikke har en aktiv eller fullført tildeling.
        var needs = await ApplyNeedFilters(
                _dbContext.Needs.AsNoTracking().AsQueryable(),
                fromDate,
                toDate,
                category,
                location)
            .Where(need => !_dbContext.MatchRecords.Any(match =>
                match.NeedId == need.Id &&
                (match.Status == "accepted" || match.Status == "completed")))
            .OrderBy(need => need.RegisteredAt)
            .Select(need => new
            {
                need.Id,
                need.Title,
                CategoryName = need.CategoryInfo!.CategoryName,
                need.LocationName,
                need.Priority,
                need.RegisteredAt,
                need.Deadline
            })
            .ToListAsync();

        // Velg bare feltene rapporttabellen trenger, ikke hele databaseobjektet.
        return needs.Select(need => new ReportsIndexViewModel.UnansweredNeedRowViewModel
        {
            Id = need.Id,
            Title = need.Title,
            Category = CategoryLabelHelper.DisplayName(need.CategoryName),
            LocationName = need.LocationName,
            Priority = need.Priority.ToString(),
            RegisteredAt = need.RegisteredAt,
            Deadline = need.Deadline
        }).ToList();
    }

    private static IQueryable<ResourceNeed> ApplyNeedFilters(
        IQueryable<ResourceNeed> needs,
        DateTime? fromDate,
        DateTime? toDate,
        int? category,
        string? location)
    {
        // Hvert filter brukes uavhengig. Dermed kan rapporten filtreres på ulike egenskaper
        if (fromDate.HasValue)
        {
            var startDate = fromDate.Value.Date;
            needs = needs.Where(need => need.RegisteredAt >= startDate);
        }

        if (toDate.HasValue)
        {
            // Inkluder hele sluttdatoen ved å bruke starten på neste dag som grense.
            var endDateExclusive = toDate.Value.Date.AddDays(1);
            needs = needs.Where(need => need.RegisteredAt < endDateExclusive);
        }

        if (category.HasValue)
        {
            needs = needs.Where(need => need.Category == category.Value);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            needs = needs.Where(need => need.LocationName.Contains(location));
        }

        return needs;
    }
}
