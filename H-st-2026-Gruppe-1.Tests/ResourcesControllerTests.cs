using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Mvc;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for ResourcesController: the "My resources" page.
// A resource provider may only see their own resources. Public actors and administrators see all.
public class ResourcesControllerTests : IDisposable
{
    private readonly TestDb _db = new TestDb();

    public ResourcesControllerTests()
    {
        // Provider 2 owns resources 1 and 2. Provider 3 owns resource 3.
        _db.Seed(c =>
        {
            c.Categories.Add(TestData.NewCategory(1, "Transport"));
            c.Categories.Add(TestData.NewCategory(2, "Aggregat"));

            var generator = TestData.NewResource(1, providerId: 2, categoryId: 2, name: "Aggregat A", status: "available");
            generator.AvailableFrom = new DateTime(2026, 11, 1);
            var tractor = TestData.NewResource(2, providerId: 2, categoryId: 1, name: "Traktor B", status: "unavailable");
            tractor.AvailableFrom = new DateTime(2026, 11, 10);
            var excavator = TestData.NewResource(3, providerId: 3, categoryId: 1, name: "Gravemaskin C", status: "available");
            excavator.AvailableFrom = new DateTime(2026, 11, 20);

            c.Resources.AddRange(generator, tractor, excavator);
        });
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ---------- Helpers ----------

    private ResourcesController NewController(int? userId, string? role)
    {
        var controller = new ResourcesController(_db.CreateContext());
        ControllerTestHelper.Attach(controller, userId, role);
        return controller;
    }

    // Calls Index with only the filters the test cares about
    private static IActionResult CallIndex(
        ResourcesController controller,
        int? id = null,
        string? search = null,
        string? status = null,
        int? resourceType = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        return controller.Index(id, search, status, resourceType, startDate, endDate);
    }

    private static ResourceDashboard DashboardFrom(IActionResult result)
    {
        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsType<ResourceDashboard>(view.Model);
    }

    private static int[] IdsIn(ResourceDashboard dashboard)
    {
        return dashboard.Resources.Select(r => r.Id).OrderBy(id => id).ToArray();
    }

    // ---------- Who can see what ----------

    [Fact]
    public void Provider_OnlySeesOwnResources()
    {
        var controller = NewController(2, "CivilianProvider");

        var dashboard = DashboardFrom(CallIndex(controller));

        Assert.Equal(new[] { 1, 2 }, IdsIn(dashboard));
    }

    [Fact]
    public void PublicActor_SeesAllResources()
    {
        var controller = NewController(1, "PublicActor");

        var dashboard = DashboardFrom(CallIndex(controller));

        Assert.Equal(new[] { 1, 2, 3 }, IdsIn(dashboard));
    }

    [Fact]
    public void Provider_WithoutAUserIdClaim_IsUnauthorized()
    {
        var controller = NewController(null, "CivilianProvider");

        var result = CallIndex(controller);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // ---------- Counters ----------

    [Fact]
    public void Counters_ExpiredResourceIsCountedAsExpired()
    {
        _db.Seed(c => c.Resources.Add(TestData.NewResource(10, providerId: 5, name: "Gammel", status: "available")));
        SetExpiration(10, DateTime.Today.AddDays(-1));
        var controller = NewController(5, "CivilianProvider");

        var dashboard = DashboardFrom(CallIndex(controller));

        Assert.Equal(1, dashboard.ExpiredCount);
        Assert.Equal(0, dashboard.ExpiringSoonCount);
    }

    private void SetExpiration(int resourceId, DateTime date)
    {
        using var context = _db.CreateContext();
        context.Resources.Single(r => r.Id == resourceId).ExpirationDate = date;
        context.SaveChanges();
    }

    // ---------- Filters ----------

    [Fact]
    public void Search_FindsResourcesByName()
    {
        var controller = NewController(1, "PublicActor");

        var dashboard = DashboardFrom(CallIndex(controller, search: "Traktor"));

        Assert.Equal(new[] { 2 }, IdsIn(dashboard));
    }

    [Fact]
    public void StatusFilter_Available_LeavesOutUnavailable()
    {
        var controller = NewController(1, "PublicActor");

        var dashboard = DashboardFrom(CallIndex(controller, status: "available"));

        Assert.Equal(new[] { 1, 3 }, IdsIn(dashboard));
    }

    // ---------- Editing one resource (id in the address) ----------

    [Fact]
    public void Id_OfAnotherProvidersResource_IsNotFound()
    {
        var controller = NewController(2, "CivilianProvider");

        var result = CallIndex(controller, id: 3);

        Assert.IsType<NotFoundResult>(result);
    }
}
