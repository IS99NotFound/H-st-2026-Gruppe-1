using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for HomeController: the page for resource providers (register, edit and delete resources).
// Providers may only change their own resources. Administrators may change all.
public class HomeControllerTests : IDisposable
{
    private readonly TestDb _db = new TestDb();

    public HomeControllerTests()
    {
        _db.Seed(c => c.Categories.Add(TestData.NewCategory(1, "Transport")));
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ---------- Helpers ----------

    private HomeController NewController(int? userId, string? role)
    {
        var context = _db.CreateContext();
        var controller = new HomeController(context, new MatchNeedLifecycle(context));
        ControllerTestHelper.Attach(controller, userId, role);
        return controller;
    }

    private void AddResource(int id, int providerId = 2, string name = "Aggregat")
    {
        _db.Seed(c => c.Resources.Add(TestData.NewResource(id, providerId: providerId, name: name)));
    }

    private ResourceEntry? ReadResource(int id)
    {
        using var context = _db.CreateContext();
        return context.Resources.AsNoTracking().SingleOrDefault(r => r.Id == id);
    }

    private int ResourceCount()
    {
        using var context = _db.CreateContext();
        return context.Resources.Count();
    }

    // A resource as it arrives from the form
    private static ResourceEntry FormResource(int id = 0, string name = "Ny traktor")
    {
        return new ResourceEntry
        {
            Id = id,
            Name = name,
            Email = "ny@test.no",
            PhoneNumber = "+47 91234567",
            CategoryId = 1,
            Description = "Ny beskrivelse",
            Latitude = 58.5,
            Longitude = 8.5,
            LocationName = "Lillesand",
            AvailableFrom = new DateTime(2026, 12, 1),
            ExpirationDate = new DateTime(2026, 12, 24)
        };
    }

    // ---------- Index ----------

    [Fact]
    public void Index_PublicActor_IsSentToTheirOwnDashboard()
    {
        var controller = NewController(1, "PublicActor");

        var result = controller.Index(null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("DashboardIndex", redirect.ActionName);
        Assert.Equal("PublicActor", redirect.ControllerName);
    }

    [Fact]
    public void Index_ProviderWithoutUserIdClaim_IsUnauthorized()
    {
        var controller = NewController(null, "CivilianProvider");

        Assert.IsType<UnauthorizedResult>(controller.Index(null));
    }

    [Fact]
    public void Index_AnotherProvidersResourceId_IsForbidden()
    {
        AddResource(1, providerId: 3);
        var controller = NewController(2, "CivilianProvider");

        Assert.IsType<ForbidResult>(controller.Index(1));
    }

    [Fact]
    public void Index_ProviderFeedOnlyHasOwnResources()
    {
        AddResource(1, providerId: 2);
        AddResource(2, providerId: 2);
        AddResource(3, providerId: 3);
        var controller = NewController(2, "CivilianProvider");

        controller.Index(null);

        var feed = Assert.IsType<List<OperationalFeedItem>>(controller.ViewData["FeedResources"]);
        Assert.Equal(new[] { 1, 2 }, feed.Select(item => item.Id).OrderBy(id => id).ToArray());
        var needs = Assert.IsType<List<OperationalFeedItem>>(controller.ViewData["FeedNeeds"]);
        Assert.Empty(needs);
    }

    // ---------- RegisterResource: new resource ----------

    [Fact]
    public void RegisterResource_NewResourceByProvider_IsSavedAndGoesToMyResources()
    {
        var controller = NewController(2, "CivilianProvider");

        var result = controller.RegisterResource(FormResource());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Resources", redirect.ControllerName);
        Assert.Equal(1, ResourceCount());
    }

    [Fact]
    public void RegisterResource_NewResourceByAdministrator_IsForbidden()
    {
        var controller = NewController(99, "Administrator");

        var result = controller.RegisterResource(FormResource());

        Assert.IsType<ForbidResult>(result);
        Assert.Equal(0, ResourceCount());
    }

    [Fact]
    public void RegisterResource_InvalidForm_ShowsTheFormAgainAndSavesNothing()
    {
        var controller = NewController(2, "CivilianProvider");
        controller.ModelState.AddModelError("Name", "Fullt navn er påkrevd.");
        var form = FormResource();

        var result = controller.RegisterResource(form);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.Same(form, view.Model);
        Assert.Equal(0, ResourceCount());
    }

    // ---------- RegisterResource: editing ----------

    [Fact]
    public void RegisterResource_EditAnotherProvidersResource_IsForbiddenAndNothingChanges()
    {
        AddResource(1, providerId: 3, name: "Andres ressurs");
        var controller = NewController(2, "CivilianProvider");

        var result = controller.RegisterResource(FormResource(id: 1, name: "Overtatt"));

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("Andres ressurs", ReadResource(1)!.Name);
    }

    // ---------- DeleteResource ----------

    [Fact]
    public async Task DeleteResource_Owner_WithdrawsTheResourceInsteadOfRemovingIt()
    {
        AddResource(1, providerId: 2);
        AddResource(2, providerId: 2);
        var controller = NewController(2, "CivilianProvider");

        var result = await controller.DeleteResource(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        // The row stays in the database so the match history is kept
        Assert.Equal("withdrawn", ReadResource(1)!.Status);
        Assert.Equal("available", ReadResource(2)!.Status);
    }

    [Fact]
    public async Task DeleteResource_AnotherProvider_IsForbiddenAndTheResourceStays()
    {
        AddResource(1, providerId: 3);
        var controller = NewController(2, "CivilianProvider");

        var result = await controller.DeleteResource(1);

        Assert.IsType<ForbidResult>(result);
        Assert.NotNull(ReadResource(1));
    }

    // ---------- DeleteNeed ----------

    // A need owned by public actor 1, with resource 1 assigned (accepted) to it
    private void AddNeedWithAssignedResource()
    {
        _db.Seed(c =>
        {
            // Deleting a need is allowed for actors in the same organization as the owner
            c.Organizations.Add(new Organization { OrganizationId = 1, OrganizationName = "Kommunen" });
            c.PublicActors.Add(new PublicActor { PublicActorId = 1, OrganizationId = 1 });
            c.Resources.Add(TestData.NewResource(1, status: "unavailable"));
            c.Needs.Add(TestData.NewNeed(1, publicActorId: 1, status: "assigned"));
            c.MatchRecords.Add(TestData.NewMatch(1, 1, "accepted"));
        });
    }

    private ResourceNeed? ReadNeed(int id)
    {
        using var context = _db.CreateContext();
        return context.Needs.AsNoTracking().SingleOrDefault(n => n.Id == id);
    }

    [Fact]
    public async Task DeleteNeed_CancelsTheNeedKeepsTheHistoryAndFreesTheAssignedResource()
    {
        AddNeedWithAssignedResource();
        var controller = NewController(1, "PublicActor");

        await controller.DeleteNeed(1);

        using var context = _db.CreateContext();
        Assert.Equal("cancelled", context.Needs.Single(n => n.Id == 1).Status);
        Assert.Equal("completed", context.MatchRecords.Single().Status);
        Assert.Equal("available", context.Resources.Single(r => r.Id == 1).Status);
    }

    // ---------- Error page ----------
}
