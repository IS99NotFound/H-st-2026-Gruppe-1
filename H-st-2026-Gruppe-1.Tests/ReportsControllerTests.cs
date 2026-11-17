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
        Assert.Contains("Navn;Kategori;Beskrivelse;Region;Sted;E-post;Telefon;Status;Registrert", csv);
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

    // ---------- Region filter ----------

    // Adds one incident, need and resource in Agder (8) and one of each in Rogaland (9)
    private void AddTwoRegions()
    {
        _db.Seed(context =>
        {
            context.Regions.Add(new Region { RegionId = 8, RegionName = "Agder" });
            context.Regions.Add(new Region { RegionId = 9, RegionName = "Rogaland" });

            context.Incidents.Add(TestData.NewIncident(1, title: "Storm Agder"));
            context.Incidents.Add(TestData.NewIncident(2, title: "Flom Rogaland"));
            context.Incidents.Find(1)!.RegionId = 8;
            context.Incidents.Find(2)!.RegionId = 9;

            context.Needs.Add(TestData.NewNeed(1, title: "Behov Agder"));
            context.Needs.Add(TestData.NewNeed(2, title: "Behov Rogaland"));
            context.Needs.Find(1)!.RegionId = 8;
            context.Needs.Find(2)!.RegionId = 9;

            var rogalandResource = TestData.NewResource(2, name: "Beltevogn");
            rogalandResource.RegionId = 9;
            context.Resources.Add(rogalandResource);
        });
    }

    [Fact]
    public async Task Index_WithRegion_OnlyCountsThatRegion()
    {
        AddTwoRegions();

        var result = await NewController().Index(9);

        var model = Assert.IsType<ReportsIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(1, model.TotalIncidents);
        Assert.Equal(1, model.TotalNeeds);
        Assert.Equal(1, model.TotalResources);
    }

    [Fact]
    public async Task Index_WithoutRegion_CountsEverything()
    {
        AddTwoRegions();

        var result = await NewController().Index();

        var model = Assert.IsType<ReportsIndexViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(2, model.TotalIncidents);
        Assert.Equal(2, model.TotalNeeds);
        Assert.Equal(2, model.TotalResources);
    }

    [Fact]
    public void NeedsView_WithRegion_OnlyShowsNeedsFromThatRegion()
    {
        AddTwoRegions();

        var result = NewController().NeedsView(9);

        var view = Assert.IsType<ViewResult>(result);
        var need = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<NeedReportViewModel>>(view.Model));
        Assert.Equal("Behov Rogaland", need.Title);
        Assert.Equal("Rogaland", need.RegionName);
    }

    [Fact]
    public void ResourcesView_WithRegion_OnlyShowsResourcesFromThatRegion()
    {
        AddTwoRegions();

        var result = NewController().ResourcesView(9);

        var view = Assert.IsType<ViewResult>(result);
        var resource = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<ResourceReportRow>>(view.Model));
        Assert.Equal("Beltevogn", resource.Name);
        Assert.Equal("Rogaland", resource.RegionName);
    }

    [Fact]
    public void IncidentsView_WithRegion_OnlyShowsIncidentsFromThatRegion()
    {
        AddTwoRegions();

        var result = NewController().IncidentsView(8);

        var view = Assert.IsType<ViewResult>(result);
        var incident = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<IncidentReportRow>>(view.Model));
        Assert.Equal("Storm Agder", incident.Title);
    }

    [Fact]
    public void Needs_Csv_HasRegionColumnAndOnlyRowsFromTheChosenRegion()
    {
        AddTwoRegions();

        var file = Assert.IsType<FileContentResult>(NewController().Needs(8));

        var csv = System.Text.Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("Tittel;Kategori;Beskrivelse;Region;Sted", csv);
        Assert.Contains("\"Behov Agder\"", csv);
        Assert.DoesNotContain("Behov Rogaland", csv);
    }

    [Fact]
    public void IncidentsJson_WithRegionWithoutData_ReturnsEmptyList()
    {
        AddTwoRegions();

        var file = Assert.IsType<FileContentResult>(NewController().IncidentsJson(3));

        using var document = JsonDocument.Parse(file.FileContents);
        Assert.Empty(document.RootElement.EnumerateArray());
    }

    private ReportsController NewController()
    {
        var controller = new ReportsController(_db.CreateContext());
        ControllerTestHelper.Attach(controller, 1, "PublicActor");
        return controller;
    }
}
