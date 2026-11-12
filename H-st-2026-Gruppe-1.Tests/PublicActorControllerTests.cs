using System.Text.Json;
using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for PublicActorController: the pages for public actors (needs, map markers and matching).
// The class is in the file HomeController.cs.
public class PublicActorControllerTests : IDisposable
{
    private readonly TestDb _db = new TestDb();

    public PublicActorControllerTests()
    {
        _db.Seed(c =>
        {
            c.Categories.Add(TestData.NewCategory(1, "Transport"));
            c.Categories.Add(TestData.NewCategory(2, "Aggregat"));

            // Access is decided by organization: actors 1 and 2 work for organization 1, actor 5 for organization 2
            c.Organizations.Add(new Organization { OrganizationId = 1, OrganizationName = "Kommunen" });
            c.Organizations.Add(new Organization { OrganizationId = 2, OrganizationName = "Fylket" });
            c.PublicActors.Add(new PublicActor { PublicActorId = 1, OrganizationId = 1 });
            c.PublicActors.Add(new PublicActor { PublicActorId = 2, OrganizationId = 1 });
            c.PublicActors.Add(new PublicActor { PublicActorId = 5, OrganizationId = 2 });
        });
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ---------- Helpers ----------

    private PublicActorController NewController(int? userId, string? role)
    {
        var context = _db.CreateContext();
        var controller = new PublicActorController(context, new MatchNeedLifecycle(context));
        ControllerTestHelper.Attach(controller, userId, role);
        return controller;
    }

    private void AddNeed(int id, int publicActorId = 1, int categoryId = 1, string status = "new",
        NeedPriorityEnum priority = NeedPriorityEnum.Planned, DateTime? deadline = null)
    {
        _db.Seed(c => c.Needs.Add(TestData.NewNeed(
            id, publicActorId: publicActorId, categoryId: categoryId, status: status, priority: priority, deadline: deadline)));
    }

    private void AddResource(int id, string status = "available", int categoryId = 1,
        double? latitude = 58.1467, double? longitude = 7.9956)
    {
        _db.Seed(c => c.Resources.Add(TestData.NewResource(
            id, categoryId: categoryId, status: status, latitude: latitude, longitude: longitude)));
    }

    private void AddMatch(int needId, int resourceId, string status)
    {
        _db.Seed(c => c.MatchRecords.Add(TestData.NewMatch(needId, resourceId, status)));
    }

    private string ResourceStatus(int id)
    {
        using var context = _db.CreateContext();
        return context.Resources.AsNoTracking().Single(r => r.Id == id).Status;
    }

    private ResourceNeed ReadNeed(int id)
    {
        using var context = _db.CreateContext();
        return context.Needs.AsNoTracking().Single(n => n.Id == id);
    }

    private int NeedCount()
    {
        using var context = _db.CreateContext();
        return context.Needs.Count();
    }

    private MatchRecord? ReadMatch(int needId, int resourceId)
    {
        using var context = _db.CreateContext();
        return context.MatchRecords.AsNoTracking()
            .SingleOrDefault(m => m.NeedId == needId && m.ResourceId == resourceId);
    }

    private static JsonElement ToJson(object? value)
    {
        return JsonSerializer.SerializeToElement(value);
    }

    // A need as it arrives from the form
    private static ResourceNeed FormNeed(int id = 0, string title = "Trenger aggregat")
    {
        return new ResourceNeed
        {
            Id = id,
            Title = title,
            Email = "ny@test.no",
            PhoneNumber = "+47 91234567",
            CrisisEventId = 2,
            Category = 2,
            Description = "Ny beskrivelse",
            Quantity = 3,
            Priority = NeedPriorityEnum.Critical,
            LocationName = "Lillesand",
            Latitude = 58.5,
            Longitude = 8.5
        };
    }

    // ---------- DashboardIndex ----------

    [Fact]
    public void DashboardIndex_Provider_IsSentToHome()
    {
        var controller = NewController(2, "CivilianProvider");

        var result = controller.DashboardIndex(null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public void DashboardIndex_NeedFromAnotherOrganization_IsForbidden()
    {
        AddNeed(1, publicActorId: 5);
        var controller = NewController(1, "PublicActor");

        Assert.IsType<ForbidResult>(controller.DashboardIndex(1));
    }

    // ---------- GetResourceMarkers (JSON for the map) ----------

    [Fact]
    public void GetResourceMarkers_Default_OnlyAvailableResources()
    {
        AddResource(1);
        AddResource(2, status: "unavailable");
        var controller = NewController(1, "PublicActor");

        var result = controller.GetResourceMarkers();

        var json = ToJson(Assert.IsType<JsonResult>(result).Value);
        Assert.Equal(1, json.GetArrayLength());
        Assert.Equal("available", json[0].GetProperty("status").GetString());
    }

    // ---------- NeedsIndex ----------

    [Fact]
    public void NeedsIndex_PublicActor_OnlySeesNeedsFromTheirOwnOrganization()
    {
        AddNeed(1, publicActorId: 1);
        AddNeed(2, publicActorId: 2);
        AddNeed(3, publicActorId: 5);
        var controller = NewController(1, "PublicActor");

        var result = controller.NeedsIndex(null, null, null, null, null);

        // Need 2 belongs to a colleague in the same organization, need 3 to another organization
        var needs = Assert.IsAssignableFrom<IEnumerable<ResourceNeed>>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(new[] { 1, 2 }, needs.Select(n => n.Id).OrderBy(id => id).ToArray());
    }

    [Fact]
    public void NeedsIndex_CriticalNeedsComeBeforePlannedNeeds()
    {
        AddNeed(1, priority: NeedPriorityEnum.Planned);
        AddNeed(2, priority: NeedPriorityEnum.Critical);
        AddNeed(3, priority: NeedPriorityEnum.Planned);
        var controller = NewController(1, "PublicActor");

        var result = controller.NeedsIndex(null, null, null, null, null);

        var needs = Assert.IsAssignableFrom<IEnumerable<ResourceNeed>>(Assert.IsType<ViewResult>(result).Model).ToList();
        Assert.Equal(2, needs[0].Id);
        Assert.Equal(NeedPriorityEnum.Planned, needs[1].Priority);
        Assert.Equal(NeedPriorityEnum.Planned, needs[2].Priority);
    }

    [Fact]
    public void NeedsIndex_ResolvesNeedsWhoseDeadlineHasPassed()
    {
        AddNeed(1, status: "new", deadline: DateTime.Now.AddDays(-2));
        var controller = NewController(1, "PublicActor");

        controller.NeedsIndex(null, null, null, null, null);

        Assert.Equal("resolved", ReadNeed(1).Status);
    }

    // ---------- RegisterNeed ----------

    [Fact]
    public void RegisterNeed_NewNeedByPublicActor_IsSavedAndGoesToTheList()
    {
        var controller = NewController(1, "PublicActor");

        var result = controller.RegisterNeed(FormNeed());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("NeedsIndex", redirect.ActionName);
        Assert.Equal(1, NeedCount());
    }

    [Fact]
    public void RegisterNeed_Administrator_IsSentToHomeAndNothingIsSaved()
    {
        var controller = NewController(99, "Administrator");

        var result = controller.RegisterNeed(FormNeed());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal(0, NeedCount());
    }

    [Fact]
    public void RegisterNeed_InvalidForm_ShowsTheDashboardAgainAndSavesNothing()
    {
        var controller = NewController(1, "PublicActor");
        controller.ModelState.AddModelError("Title", "Tittel eller kort sammendrag er påkrevd.");
        var form = FormNeed();

        var result = controller.RegisterNeed(form);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("DashboardIndex", view.ViewName);
        Assert.Same(form, view.Model);
        Assert.Equal(true, controller.ViewData["OpenRegistrationModal"]);
        Assert.Equal(0, NeedCount());
    }

    [Fact]
    public void RegisterNeed_EditNeedFromAnotherOrganization_IsForbiddenAndNothingChanges()
    {
        AddNeed(1, publicActorId: 5);
        var controller = NewController(1, "PublicActor");

        var result = controller.RegisterNeed(FormNeed(id: 1, title: "Overtatt"));

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("Trenger strøm", ReadNeed(1).Title);
    }

    // ---------- UpdateResourceStatus (the Send request / Withdraw buttons) ----------

    [Fact]
    public void UpdateResourceStatus_UnknownStatusText_IsBadRequest()
    {
        AddNeed(1);
        AddResource(1);
        var controller = NewController(1, "PublicActor");

        var result = controller.UpdateResourceStatus(1, 1, "hacked", null);

        Assert.IsType<BadRequestResult>(result);
        Assert.Null(ReadMatch(1, 1));
    }

    [Fact]
    public void UpdateResourceStatus_NeedFromAnotherOrganization_IsForbidden()
    {
        AddNeed(1, publicActorId: 5);
        AddResource(1);
        var controller = NewController(1, "PublicActor");

        var result = controller.UpdateResourceStatus(1, 1, "proposed", null);

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("available", ResourceStatus(1));
        Assert.Null(ReadMatch(1, 1));
    }

    [Fact]
    public void UpdateResourceStatus_Propose_SendsTheRequestWithoutLockingTheResource()
    {
        AddNeed(1);
        AddResource(1);
        var controller = NewController(1, "PublicActor");

        var result = controller.UpdateResourceStatus(1, 1, "proposed", null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Matches", redirect.ActionName);
        Assert.Equal(1, redirect.RouteValues!["needId"]);
        Assert.Equal("proposed", ReadMatch(1, 1)!.Status);
        Assert.Equal("available", ResourceStatus(1));
        Assert.Equal("under_review", ReadNeed(1).Status);
    }

    [Fact]
    public void UpdateResourceStatus_PublicActorCannotAcceptTheirOwnRequest()
    {
        AddNeed(1);
        AddResource(1);
        var controller = NewController(1, "PublicActor");

        var result = controller.UpdateResourceStatus(1, 1, "accepted", null);

        Assert.IsType<BadRequestResult>(result);
        Assert.Null(ReadMatch(1, 1));
        Assert.Equal("available", ResourceStatus(1));
    }

    [Fact]
    public void UpdateResourceStatus_WithdrawProposal_NeedGoesBackToNew()
    {
        AddNeed(1, status: "under_review");
        AddResource(1);
        AddMatch(1, 1, "proposed");
        var controller = NewController(1, "PublicActor");

        controller.UpdateResourceStatus(1, 1, "declined", null);

        Assert.Equal("declined", ReadMatch(1, 1)!.Status);
        Assert.Equal("new", ReadNeed(1).Status);
    }

    [Theory]
    [InlineData("resolved")]
    public void UpdateResourceStatus_ClosedNeed_CannotGetNewAssignments(string needStatus)
    {
        AddNeed(1, status: needStatus);
        AddResource(1);
        var controller = NewController(1, "PublicActor");

        controller.UpdateResourceStatus(1, 1, "proposed", null);

        Assert.NotNull(controller.TempData["MatchError"]);
        Assert.Equal("available", ResourceStatus(1));
        Assert.Null(ReadMatch(1, 1));
    }

    [Fact]
    public void UpdateResourceStatus_DeclineAfterAccept_FreesTheResourceAgain()
    {
        AddNeed(1, status: "assigned");
        AddResource(1, status: "unavailable");
        AddMatch(1, 1, "accepted");
        var controller = NewController(1, "PublicActor");

        controller.UpdateResourceStatus(1, 1, "declined", null);

        Assert.Equal("available", ResourceStatus(1));
        Assert.Equal("declined", ReadMatch(1, 1)!.Status);
        Assert.Equal("new", ReadNeed(1).Status);
    }

    // ---------- ResolveNeed ----------

    [Fact]
    public void ResolveNeed_Owner_ResolvesTheNeedAndFreesTheResources()
    {
        AddNeed(1, publicActorId: 1, status: "assigned");
        AddResource(1, status: "unavailable");
        AddMatch(1, 1, "accepted");
        var controller = NewController(1, "PublicActor");

        var result = controller.ResolveNeed(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("NeedsIndex", redirect.ActionName);
        Assert.Equal("resolved", ReadNeed(1).Status);
        Assert.Equal("completed", ReadMatch(1, 1)!.Status);
        Assert.Equal("available", ResourceStatus(1));
    }

    [Fact]
    public void ResolveNeed_NeedFromAnotherOrganization_IsForbidden()
    {
        AddNeed(1, publicActorId: 5);
        var controller = NewController(1, "PublicActor");

        var result = controller.ResolveNeed(1);

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("new", ReadNeed(1).Status);
    }

    // ---------- Matches: resources near a need ----------

    // Need 1 (category 1) is in Kristiansand. This adds a typical set of resources around it.
    private void AddMatchingScenario()
    {
        AddNeed(1, publicActorId: 1, categoryId: 1);
        AddResource(1, latitude: 58.1467, longitude: 7.9956);                  // here, same category
        AddResource(2, latitude: 58.2467, longitude: 7.9956);                  // 11 km away, same category
        AddResource(3, categoryId: 2, latitude: 58.1467, longitude: 7.9956);   // here, other category
        AddResource(4, status: "unavailable");                                 // here, taken by someone else
        AddResource(5, status: "unavailable");                                 // here, assigned to this need
        AddMatch(1, 5, "accepted");
    }

    private static MatchViewModel MatchModelFrom(IActionResult result)
    {
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("MatchesIndex", view.ViewName);
        return Assert.IsType<MatchViewModel>(view.Model);
    }

    private static int[] MatchedIds(MatchViewModel model)
    {
        return model.Matches.Select(m => m.Resource.Id).OrderBy(id => id).ToArray();
    }

    [Fact]
    public void Matches_ShowsAvailableResourcesInTheNeedsCategory()
    {
        AddMatchingScenario();
        var controller = NewController(1, "PublicActor");

        var model = MatchModelFrom(controller.Matches(1, 0));

        Assert.Contains(1, MatchedIds(model));
        Assert.Contains(2, MatchedIds(model));
    }

    [Fact]
    public void Matches_LeavesOutResourcesInOtherCategories()
    {
        AddMatchingScenario();
        var controller = NewController(1, "PublicActor");

        var model = MatchModelFrom(controller.Matches(1, 0));

        Assert.DoesNotContain(3, MatchedIds(model));
    }

    [Fact]
    public void Matches_LeavesOutResourcesTakenByOthers()
    {
        AddMatchingScenario();
        var controller = NewController(1, "PublicActor");

        var model = MatchModelFrom(controller.Matches(1, 0));

        Assert.DoesNotContain(4, MatchedIds(model));
    }

    [Fact]
    public void Matches_RadiusLimitsTheResources()
    {
        AddMatchingScenario();
        var controller = NewController(1, "PublicActor");

        var model = MatchModelFrom(controller.Matches(1, 5));

        Assert.Equal(5, model.RadiusKm);
        Assert.Contains(1, MatchedIds(model));
        Assert.DoesNotContain(2, MatchedIds(model));
    }
}
