using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Helpers;

// Lager radene i den operative oversikten.
public static class OperationalFeedHelper
{
    public static List<OperationalFeedItem> GetResourceItems(IQueryable<ResourceEntry> resources)
    {
        return resources
            .Include(resource => resource.Category)
            .AsEnumerable()
            .Select(resource => new OperationalFeedItem
            {
                Type = "resource",
                Id = resource.Id,
                ProviderId = resource.ProviderId,
                Title = resource.Name,
                Description = resource.Description,
                Status = resource.Status,
                Category = resource.Category?.CategoryName,
                LocationName = resource.LocationName,
                ColorKey = resource.Status == "available"
                    ? "available-resource"
                    : "unavailable-resource",
                Latitude = resource.Latitude ?? 0,
                Longitude = resource.Longitude ?? 0,
                CreatedAt = resource.RegisteredAt
            })
            .ToList();
    }

    public static List<OperationalFeedItem> GetIncidentItems(AppDbContext db, int? organizationId)
    {
        var editableOwnerIds = db.GetOrganizationMemberIds(organizationId);

        return db.Incidents
            .Where(incident => incident.Status == "active")
            .AsEnumerable()
            .Select(incident => new OperationalFeedItem
            {
                Type = "incident",
                Id = incident.IncidentId,
                CanEdit = editableOwnerIds.Contains(incident.PublicActorId),
                Title = incident.Title,
                Description = incident.Description,
                Status = incident.Status,
                ColorKey = "public-team",
                Latitude = (double)incident.Latitude,
                Longitude = (double)incident.Longitude,
                CreatedAt = incident.CreatedAt
            })
            .ToList();
    }
}
