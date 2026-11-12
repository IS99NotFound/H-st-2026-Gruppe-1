using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        var context = _db.CreateContext();
        var controller = new ResourcesController(context, new MatchNeedLifecycle(context));
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

    // ---------- Requests from public actors (the Accept / Decline buttons) ----------

    // A need with a request (proposed) on one resource
    private void AddRequest(int needId, int resourceId, string needStatus = "under_review")
    {
        _db.Seed(c =>
        {
            c.Needs.Add(TestData.NewNeed(needId, status: needStatus));
            c.MatchRecords.Add(TestData.NewMatch(needId, resourceId, "proposed"));
        });
    }

    private string MatchStatusOf(int needId, int resourceId)
    {
        using var context = _db.CreateContext();
        return context.MatchRecords.AsNoTracking()
            .Single(m => m.NeedId == needId && m.ResourceId == resourceId).Status;
    }

    private string ResourceStatusOf(int id)
    {
        using var context = _db.CreateContext();
        return context.Resources.AsNoTracking().Single(r => r.Id == id).Status;
    }

    private string NeedStatusOf(int id)
    {
        using var context = _db.CreateContext();
        return context.Needs.AsNoTracking().Single(n => n.Id == id).Status;
    }

    [Fact]
    public void Index_Provider_SeesRequestsOnlyForOpenNeeds()
    {
        AddRequest(1, 1);
        AddRequest(2, 1, needStatus: "cancelled");
        var controller = NewController(2, "CivilianProvider");

        var dashboard = DashboardFrom(CallIndex(controller));

        var request = Assert.Single(dashboard.MatchStatuses);
        Assert.Equal(1, request.NeedId);
        Assert.Equal("proposed", request.Status);
    }

    [Fact]
    public void RespondToMatch_Accept_AssignsTheResourceAndLocksIt()
    {
        AddRequest(1, 1);
        var controller = NewController(2, "CivilianProvider");

        var result = controller.RespondToMatch(1, 1, "accepted");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("accepted", MatchStatusOf(1, 1));
        Assert.Equal("unavailable", ResourceStatusOf(1));
        Assert.Equal("assigned", NeedStatusOf(1));
    }

    [Fact]
    public void RespondToMatch_Decline_KeepsTheResourceAvailable()
    {
        AddRequest(1, 1);
        var controller = NewController(2, "CivilianProvider");

        controller.RespondToMatch(1, 1, "declined");

        Assert.Equal("declined", MatchStatusOf(1, 1));
        Assert.Equal("available", ResourceStatusOf(1));
        Assert.Equal("new", NeedStatusOf(1));
    }

    [Fact]
    public void RespondToMatch_OtherProvidersResource_IsForbidden()
    {
        AddRequest(1, 1);
        var controller = NewController(3, "CivilianProvider");

        var result = controller.RespondToMatch(1, 1, "accepted");

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("proposed", MatchStatusOf(1, 1));
    }

    [Fact]
    public void RespondToMatch_UnknownAnswer_IsBadRequest()
    {
        AddRequest(1, 1);
        var controller = NewController(2, "CivilianProvider");

        var result = controller.RespondToMatch(1, 1, "hacked");

        Assert.IsType<BadRequestResult>(result);
        Assert.Equal("proposed", MatchStatusOf(1, 1));
    }

    [Fact]
    public void RespondToMatch_ResourceAlreadyTaken_ShowsAMessageAndChangesNothing()
    {
        // Resource 2 is already unavailable (assigned to another need)
        AddRequest(1, 2);
        var controller = NewController(2, "CivilianProvider");

        controller.RespondToMatch(2, 1, "accepted");

        Assert.NotNull(controller.TempData["ResourceError"]);
        Assert.Equal("proposed", MatchStatusOf(1, 2));
        Assert.Equal("under_review", NeedStatusOf(1));
    }

    [Fact]
    public void RespondToMatch_ClosedNeed_CannotBeAnswered()
    {
        AddRequest(1, 1, needStatus: "cancelled");
        var controller = NewController(2, "CivilianProvider");

        controller.RespondToMatch(1, 1, "accepted");

        Assert.NotNull(controller.TempData["ResourceError"]);
        Assert.Equal("proposed", MatchStatusOf(1, 1));
        Assert.Equal("available", ResourceStatusOf(1));
    }
}
