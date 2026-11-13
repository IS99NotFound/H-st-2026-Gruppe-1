using System.Text.Json;
using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Mvc;

namespace H_st_2026_Gruppe_1.Tests;

public class ReportsControllerTests : IDisposable
{
    private readonly TestDb _db = new();

    public ReportsControllerTests()
    {
        _db.Seed(context =>
        {
            context.Categories.Add(TestData.NewCategory(1, "Transport"));
            context.Resources.Add(TestData.NewResource(
                1,
                name: "Traktor",
                categoryId: 1));
        });
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public void Resources_ReturnsCsvFileWithExistingFormat()
    {
        var result = NewController().Resources();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal("Ressurser.csv", file.FileDownloadName);

        var csv = System.Text.Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("Navn;Kategori;Beskrivelse;Sted;E-post;Telefon;Status;Registrert", csv);
        Assert.Contains("\"Traktor\"", csv);
    }

    [Fact]
    public void ResourcesJson_ReturnsJsonFileWithResources()
    {
        var result = NewController().ResourcesJson();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/json; charset=utf-8", file.ContentType);
        Assert.Equal("Ressurser.json", file.FileDownloadName);

        using var document = JsonDocument.Parse(file.FileContents);
        var resource = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Traktor", resource.GetProperty("name").GetString());
        Assert.Equal("Transport", resource.GetProperty("category").GetString());
    }

    [Fact]
    public void ResourcesView_ReturnsTheSharedReportRows()
    {
        var result = NewController().ResourcesView();

        var view = Assert.IsType<ViewResult>(result);
        var resources = Assert.IsAssignableFrom<IReadOnlyList<ResourceReportRow>>(view.Model);
        Assert.Equal("Traktor", Assert.Single(resources).Name);
    }

    [Fact]
    public void NeedsView_ReturnsReportRowsWithCategoryAndContactDetails()
    {
        _db.Seed(context =>
        {
            context.Needs.Add(TestData.NewNeed(1, title: "Trenger vann"));
        });

        var result = NewController().NeedsView();

        var view = Assert.IsType<ViewResult>(result);
        var need = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<NeedReportViewModel>>(view.Model));
        Assert.Equal("Trenger vann", need.Title);
        Assert.Equal("Transport", need.CategoryInfo);
        Assert.Equal("new", need.Status);
        Assert.Equal("behov@test.no", need.Email);
    }

    [Fact]
    public void IncidentsView_ReturnsReportRowsWithRegion()
    {
        _db.Seed(context =>
        {
            context.Regions.Add(new Region { RegionId = 8, RegionName = "Agder" });
            var incident = TestData.NewIncident(1, title: "Flom i sentrum");
            incident.RegionId = 8;
            context.Incidents.Add(incident);
        });

        var result = NewController().IncidentsView();

        var view = Assert.IsType<ViewResult>(result);
        var incident = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<IncidentReportRow>>(view.Model));
        Assert.Equal("Flom i sentrum", incident.Title);
        Assert.Equal("Agder", incident.RegionName);
        Assert.Equal("active", incident.Status);
    }

    [Fact]
    public async Task Index_ReturnsReportKpis()
    {
        _db.Seed(context =>
        {
            context.Incidents.Add(TestData.NewIncident(1, status: "active"));
            context.Incidents.Add(TestData.NewIncident(2, status: "resolved"));
            context.Resources.Add(TestData.NewResource(2, status: "assigned"));
            context.Needs.Add(TestData.NewNeed(1, status: "new"));
            context.Needs.Add(TestData.NewNeed(2, status: "resolved"));
        });

        var result = await NewController().Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ReportsIndexViewModel>(view.Model);
        Assert.Equal(2, model.TotalIncidents);
        Assert.Equal(1, model.ActiveIncidents);
        Assert.Equal(1, model.ResolvedIncidents);
        Assert.Equal(2, model.TotalResources);
        Assert.Equal(1, model.AvailableResources);
        Assert.Equal(2, model.TotalNeeds);
        Assert.Equal(1, model.UnansweredNeeds);
    }

    private ReportsController NewController()
    {
        var controller = new ReportsController(_db.CreateContext());
        ControllerTestHelper.Attach(controller, 1, "PublicActor");
        return controller;
    }
}
