namespace H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Models;

// Finds the resources that are within a given radius of a need.
public static class RadiusMatcher
{
    private const double EarthRadiusKm = 6371;

    // Returns resources within radiusKm of the need, nearest first.
    public static List<ResourceMatch> FindResources(ResourceNeed need, double radiusKm)
    {
        var matches = new List<ResourceMatch>();

        // DATABASE: replace ResourceStore.Resources with a database query
        foreach (var resource in ResourceStore.Resources)
        {
            // Skip resources that have no position
            if (resource.Latitude == null || resource.Longitude == null)
            {
                continue;
            }
            
            var resourceCategory = GetCategory(resource.ResourceType);
            if (!resourceCategory.HasValue || resourceCategory.Value != need.Category)
            {
                continue;
            }

            var distance = DistanceKm(need.Latitude, need.Longitude, resource.Latitude.Value, resource.Longitude.Value);

            if (distance <= radiusKm)
            {
                matches.Add(new ResourceMatch { Resource = resource, DistanceKm = distance });
            }
        }

        // Nearest first
        return matches.OrderBy(m => m.DistanceKm).ToList();
    }

    private static int? GetCategory(string resourceType)
    {
        if (int.TryParse(resourceType, out var category) && category is >= 1 and <= 8)
        {
            return category;
        }

        return resourceType switch
        {
            "Transport/logistics" or "ATV" or "Truck" or "Boat" => 1,
            "Heavy machinery" or "Tractor" or "Excavator" => 2,
            "Power Supply / Generator" or "Generator" => 3,
            "Evacuation / Shelter" or "Shelter" => 4,
            "Plowing & Snow Clearing" => 5,
            "Manpower" or "Crew" => 6,
            "Medical" => 7,
            "Other" or "Sand/Gravel" or "Drone" => 8,
            _ => null
        };
    }

    // Distance in km between two points on the earth (haversine formula).
    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(a));
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
