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
        Assert.Equal("ressurser.csv", file.FileDownloadName);

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
        Assert.Equal("ressurser.json", file.FileDownloadName);

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

    private ReportsController NewController()
    {
        var controller = new ReportsController(_db.CreateContext());
        ControllerTestHelper.Attach(controller, 1, "PublicActor");
        return controller;
    }
}
