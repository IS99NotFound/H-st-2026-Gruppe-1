using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for IncidentsController: creating, editing, resolving and reactivating incidents (hendelser).
public class IncidentsControllerTests : IDisposable
{
    private readonly TestDb _db = new TestDb();

    public void Dispose()
    {
        _db.Dispose();
    }

    // ---------- Helpers ----------

    private IncidentsController NewController(int? userId, string? role)
    {
        var controller = new IncidentsController(_db.CreateContext());
        ControllerTestHelper.Attach(controller, userId, role);
        return controller;
    }

    private static IActionResult CallIndex(
        IncidentsController controller,
        string? search = null,
        string? status = null,
        string? incidentType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? id = null)
    {
        return controller.Index(search, status, incidentType, startDate, endDate, id);
    }

    private static IncidentIndexViewModel ModelFrom(IActionResult result)
    {
        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsType<IncidentIndexViewModel>(view.Model);
    }

    private static IncidentIndexViewModel NewCreateModel(
        string title = "  Flom i sentrum  ",
        string? description = "  Vann i gatene  ")
    {
        return new IncidentIndexViewModel
        {
            CreateModel = new IncidentCreateViewModel
            {
                Title = title,
                IncidentType = "Flood",
                Description = description,
                Latitude = 58.1467m,
                Longitude = 7.9956m
            }
        };
    }

    private Incident ReadIncident(int id)
    {
        using var context = _db.CreateContext();
        return context.Incidents.AsNoTracking().Single(i => i.IncidentId == id);
    }

    private int IncidentCount()
    {
        using var context = _db.CreateContext();
        return context.Incidents.Count();
    }

    // ---------- Index: list and filters ----------

    [Fact]
    public void Index_ShowsAllIncidents()
    {
        _db.Seed(c =>
        {
            c.Incidents.Add(TestData.NewIncident(1));
            c.Incidents.Add(TestData.NewIncident(2));
        });
        var controller = NewController(1, "PublicActor");

        var model = ModelFrom(CallIndex(controller));

        Assert.Equal(2, model.Incidents.Count);
    }

    [Fact]
    public void Index_ResolvedIncidentsAreShownLast()
    {
        _db.Seed(c =>
        {
            c.Incidents.Add(TestData.NewIncident(1, status: "resolved", createdAt: new DateTime(2026, 11, 5)));
            c.Incidents.Add(TestData.NewIncident(2, status: "active", createdAt: new DateTime(2026, 11, 1)));
        });
        var controller = NewController(1, "PublicActor");

        var model = ModelFrom(CallIndex(controller));

        Assert.Equal(new[] { 2, 1 }, model.Incidents.Select(i => i.IncidentId).ToArray());
    }

    [Fact]
    public void Index_StatusFilterOnlyShowsThatStatus()
    {
        _db.Seed(c =>
        {
            c.Incidents.Add(TestData.NewIncident(1, status: "active"));
            c.Incidents.Add(TestData.NewIncident(2, status: "resolved"));
        });
        var controller = NewController(1, "PublicActor");

        var model = ModelFrom(CallIndex(controller, status: "resolved"));

        Assert.Equal(new[] { 2 }, model.Incidents.Select(i => i.IncidentId).ToArray());
    }

    // ---------- Index: opening one incident for editing ----------

    [Theory]
    [InlineData("Administrator")]
    public void Index_WithId_OtherRolesCannotOpenTheEditForm(string role)
    {
        _db.Seed(c => c.Incidents.Add(TestData.NewIncident(1)));
        var controller = NewController(5, role);

        var result = CallIndex(controller, id: 1);

        Assert.IsType<NotFoundResult>(result);
    }

    // ---------- Create ----------

    [Fact]
    public void Create_ValidIncident_IsSavedAndRedirectsToTheList()
    {
        var controller = NewController(5, "PublicActor");

        var result = controller.Create(NewCreateModel());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(1, IncidentCount());
    }

    [Fact]
    public void Create_SavesTheOwnerAndCleansTheTexts()
    {
        var controller = NewController(5, "PublicActor");

        controller.Create(NewCreateModel());

        var incident = ReadIncident(1);
        Assert.Equal(5, incident.PublicActorId);
        Assert.Equal("Flom i sentrum", incident.Title);
        Assert.Equal("flom i sentrum", incident.NormalizedTitle);
        Assert.Equal("Vann i gatene", incident.Description);
        Assert.Equal("Flood", incident.IncidentType);
    }

    [Fact]
    public void Create_InvalidForm_ShowsTheFormAgainAndSavesNothing()
    {
        _db.Seed(c => c.Incidents.Add(TestData.NewIncident(1)));
        var controller = NewController(5, "PublicActor");
        controller.ModelState.AddModelError("CreateModel.Title", "Dette feltet er påkrevd.");

        var result = controller.Create(NewCreateModel());

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        var model = Assert.IsType<IncidentIndexViewModel>(view.Model);
        Assert.Single(model.Incidents);
        Assert.Equal(1, IncidentCount());
    }

    // ---------- Edit ----------

    private static IncidentCreateViewModel NewEditModel(int id = 1, string title = "  Nytt navn  ")
    {
        return new IncidentCreateViewModel
        {
            IncidentId = id,
            Title = title,
            IncidentType = "Fire",
            Description = "  Ny beskrivelse  ",
            Latitude = 59.5m,
            Longitude = 8.5m
        };
    }

    [Fact]
    public void Edit_PublicActor_UpdatesTheIncident()
    {
        _db.Seed(c => c.Incidents.Add(TestData.NewIncident(1, title: "Gammelt navn")));
        var controller = NewController(1, "PublicActor");

        var result = controller.Edit(NewEditModel());

        Assert.IsType<RedirectToActionResult>(result);
        var incident = ReadIncident(1);
        Assert.Equal("Nytt navn", incident.Title);
        Assert.Equal("nytt navn", incident.NormalizedTitle);
        Assert.Equal("Fire", incident.IncidentType);
        Assert.Equal("Ny beskrivelse", incident.Description);
        Assert.Equal(59.5m, incident.Latitude);
        Assert.Equal(8.5m, incident.Longitude);
    }

    [Fact]
    public void Edit_Administrator_IsForbidden()
    {
        _db.Seed(c => c.Incidents.Add(TestData.NewIncident(1, title: "Gammelt navn")));
        var controller = NewController(1, "Administrator");

        var result = controller.Edit(NewEditModel());

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("Gammelt navn", ReadIncident(1).Title);
    }

    // ---------- MarkAsResolved ----------

    [Fact]
    public void MarkAsResolved_Owner_SetsStatusToResolved()
    {
        _db.Seed(c => c.Incidents.Add(TestData.NewIncident(1, publicActorId: 5)));
        var controller = NewController(5, "PublicActor");

        var result = controller.MarkAsResolved(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("resolved", ReadIncident(1).Status);
        Assert.Equal("Hendelsen er markert som løst.", controller.TempData["IncidentUpdated"]);
    }

    [Fact]
    public void MarkAsResolved_AnotherPublicActor_IsForbidden()
    {
        _db.Seed(c => c.Incidents.Add(TestData.NewIncident(1, publicActorId: 5)));
        var controller = NewController(6, "PublicActor");

        var result = controller.MarkAsResolved(1);

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("active", ReadIncident(1).Status);
    }

    // ---------- Reactivate ----------
}
