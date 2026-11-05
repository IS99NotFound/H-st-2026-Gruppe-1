using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for RadiusMatcher: the distance calculation and the "find resources near a need" logic.
public class RadiusMatcherTests
{
    // Kristiansand is used as the position of the need in most tests
    private const double NeedLat = 58.1467;
    private const double NeedLng = 7.9956;

    private static ResourceNeed MakeNeed(int category = 1)
    {
        return new ResourceNeed { Id = 1, Category = category, Latitude = NeedLat, Longitude = NeedLng };
    }

    private static ResourceEntry MakeResource(int id, double? lat, double? lng, int categoryId = 1)
    {
        return new ResourceEntry { Id = id, CategoryId = categoryId, Latitude = lat, Longitude = lng };
    }

    // ---------- DistanceKm ----------

    [Fact]
    public void DistanceKm_OsloToBergen_IsAbout305Km()
    {
        var distance = RadiusMatcher.DistanceKm(59.9139, 10.7522, 60.3913, 5.3221);

        Assert.InRange(distance, 300, 310);
    }

    [Theory]
    [InlineData(58.1467, 7.9956, 59.9139, 10.7522)]
    [InlineData(0, 0, 10, 10)]
    public void DistanceKm_IsTheSameInBothDirections(double lat1, double lon1, double lat2, double lon2)
    {
        var forward = RadiusMatcher.DistanceKm(lat1, lon1, lat2, lon2);
        var backward = RadiusMatcher.DistanceKm(lat2, lon2, lat1, lon1);

        Assert.Equal(forward, backward, 6);
    }

    // ---------- FindResources ----------

    [Fact]
    public void FindResources_ResourceInsideRadius_IsIncluded()
    {
        // About 11.1 km north of the need
        var resources = new List<ResourceEntry> { MakeResource(1, 58.2467, NeedLng) };

        var result = RadiusMatcher.FindResources(MakeNeed(), 12, resources);

        Assert.Single(result);
        Assert.Equal(1, result[0].Resource.Id);
    }

    [Fact]
    public void FindResources_ResourceOutsideRadius_IsLeftOut()
    {
        var resources = new List<ResourceEntry> { MakeResource(1, 58.2467, NeedLng) };

        var result = RadiusMatcher.FindResources(MakeNeed(), 10, resources);

        Assert.Empty(result);
    }

    [Fact]
    public void FindResources_RadiusZero_MeansNoLimit()
    {
        // Sydney is on the other side of the world, but radius 0 means "all distances"
        var resources = new List<ResourceEntry>
        {
            MakeResource(1, 58.2467, NeedLng),
            MakeResource(2, -33.8688, 151.2093)
        };

        var result = RadiusMatcher.FindResources(MakeNeed(), 0, resources);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FindResources_ResourceWithoutLatitude_IsSkipped()
    {
        var resources = new List<ResourceEntry> { MakeResource(1, null, NeedLng) };

        var result = RadiusMatcher.FindResources(MakeNeed(), 0, resources);

        Assert.Empty(result);
    }

    [Fact]
    public void FindResources_OtherCategory_IsLeftOutByDefault()
    {
        var resources = new List<ResourceEntry>
        {
            MakeResource(1, NeedLat, NeedLng, categoryId: 1),
            MakeResource(2, NeedLat, NeedLng, categoryId: 2)
        };

        var result = RadiusMatcher.FindResources(MakeNeed(category: 1), 10, resources);

        Assert.Single(result);
        Assert.Equal(1, result[0].Resource.Id);
    }

    [Fact]
    public void FindResources_SortsNearestFirst()
    {
        var resources = new List<ResourceEntry>
        {
            MakeResource(1, 58.4467, NeedLng), // about 33 km away
            MakeResource(2, 58.2467, NeedLng), // about 11 km away
            MakeResource(3, 58.3467, NeedLng)  // about 22 km away
        };

        var result = RadiusMatcher.FindResources(MakeNeed(), 0, resources);

        Assert.Equal(new[] { 2, 3, 1 }, result.Select(match => match.Resource.Id).ToArray());
    }
}
