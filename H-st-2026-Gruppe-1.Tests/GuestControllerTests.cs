using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Mvc;

namespace H_st_2026_Gruppe_1.Tests;

public class GuestControllerTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public void Index_ReturnsOnlyActiveIncidentsAsGeneralRegionSummaries()
    {
        _db.Seed(context =>
        {
            context.Regions.Add(new Region { RegionId = 1, RegionName = "Agder" });
            var activeIncident = TestData.NewIncident(
                1,
                title: "Storm ved en sensitiv lokasjon",
                incidentType: "Storm",
                status: "active");
            activeIncident.RegionId = 1;
            context.Incidents.Add(activeIncident);
            context.Incidents.Add(TestData.NewIncident(
                2,
                title: "Ikke publiser denne",
                incidentType: "Brann",
                status: "resolved"));
        });

        var controller = new GuestController(_db.CreateContext());
        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
        var incidents = Assert.IsAssignableFrom<IReadOnlyList<OperationalFeedItem>>(
            controller.ViewData["FeedIncidents"]);
        var incident = Assert.Single(incidents);
        Assert.Equal("Storm i Agder", incident.Title);
        Assert.Equal("Agder", incident.LocationName);
        Assert.DoesNotContain("sensitiv", incident.Title);
        Assert.Null(controller.ViewData["FeedResources"]);
    }
}
