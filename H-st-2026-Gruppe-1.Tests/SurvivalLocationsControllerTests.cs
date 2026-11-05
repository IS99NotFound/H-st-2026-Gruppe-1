using System.Net;
using System.Text;
using System.Text.Json;
using H_st_2026_Gruppe_1.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for SurvivalLocationsController (hospitals and drinking water on the map).
// The real Geoapify website is never called. A fake HttpClient gives the answers instead.
public class SurvivalLocationsControllerTests
{
    // ---------- Helpers ----------

    private static IConfiguration NewConfig(string? apiKey = "test-key")
    {
        var values = new Dictionary<string, string?>();
        if (apiKey != null)
        {
            values["Geoapify:ApiKey"] = apiKey;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static SurvivalLocationsController NewController(
        StubHttpMessageHandler handler, string? apiKey = "test-key")
    {
        return new SurvivalLocationsController(new StubHttpClientFactory(handler), NewConfig(apiKey));
    }

    // A handler that always answers with the given JSON
    private static StubHttpMessageHandler HandlerReturning(string json)
    {
        return new StubHttpMessageHandler(_ => JsonResponse(json));
    }

    // One Geoapify "feature" (a place) as JSON text
    private static string Feature(string propertiesJson)
    {
        return "{\"type\":\"Feature\",\"properties\":" + propertiesJson + "}";
    }

    private static string FeatureCollection(params string[] features)
    {
        return "{\"type\":\"FeatureCollection\",\"features\":[" + string.Join(",", features) + "]}";
    }

    private static string Hospital(string name = "Sørlandet sykehus", double lat = 58.15, double lon = 8.0)
    {
        return Feature("{\"name\":\"" + name + "\",\"formatted\":\"" + name + ", Kristiansand\",\"lat\":"
            + lat.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"lon\":"
            + lon.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ",\"categories\":[\"healthcare\",\"healthcare.hospital\"]}");
    }

    private static string Water(string name = "Drikkevann", double lat = 58.16, double lon = 8.01)
    {
        return Feature("{\"name\":\"" + name + "\",\"lat\":"
            + lat.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"lon\":"
            + lon.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ",\"categories\":[\"amenity\",\"amenity.drinking_water\"]}");
    }

    // Whole-number bounds are used on purpose, so the URL does not depend on the computer's language settings.
    private static Task<IActionResult> Call(
        SurvivalLocationsController controller,
        string categories = "hospital,drinking_water",
        CancellationToken token = default)
    {
        return controller.GetLocations(58, 7, 59, 9, categories, token);
    }

    private static JsonElement ToJson(object? value)
    {
        return JsonSerializer.SerializeToElement(value);
    }

    private static JsonElement OkBody(IActionResult result)
    {
        return ToJson(Assert.IsType<OkObjectResult>(result).Value);
    }

    private static string? MessageOf(IActionResult result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        return ToJson(objectResult.Value).GetProperty("message").GetString();
    }

    // ---------- Map bounds (input checks happen before any web call) ----------

    [Theory]
    [InlineData(59, 7, 58, 9)]     // south is bigger than north
    [InlineData(58, 9, 59, 7)]     // west is bigger than east
    public async Task GetLocations_InvalidBounds_IsBadRequest(double south, double west, double north, double east)
    {
        var handler = HandlerReturning(FeatureCollection());
        var controller = NewController(handler);

        var result = await controller.GetLocations(south, west, north, east, "hospital", default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid map bounds.", MessageOf(result));
        Assert.Empty(handler.Requests);
    }

    // ---------- Categories ----------

    [Theory]
    [InlineData("")]
    [InlineData(",")]
    public async Task GetLocations_InvalidCategories_IsBadRequest(string categories)
    {
        var handler = HandlerReturning(FeatureCollection());
        var controller = NewController(handler);

        var result = await Call(controller, categories);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Select hospital or drinking_water.", MessageOf(result));
        Assert.Empty(handler.Requests);
    }

    // ---------- API key ----------

    [Theory]
    [InlineData(null)]
    public async Task GetLocations_MissingApiKey_Gives500AndNoWebCall(string? apiKey)
    {
        var handler = HandlerReturning(FeatureCollection());
        var controller = NewController(handler, apiKey);

        var result = await Call(controller);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("Geoapify API key is not configured.", MessageOf(result));
        Assert.Empty(handler.Requests);
    }

    // ---------- The web request that is sent ----------

    [Fact]
    public async Task GetLocations_UrlHasTheApiKeyLimitAndLanguage()
    {
        var handler = HandlerReturning(FeatureCollection());
        var controller = NewController(handler, "my secret/key");

        await Call(controller);

        var url = handler.Requests.Single().RequestUri!.AbsoluteUri;
        Assert.Contains("apiKey=my%20secret%2Fkey", url);
        Assert.Contains("limit=200", url);
        Assert.Contains("lang=en", url);
    }

    [Fact]
    public async Task GetLocations_UrlHasTheBoundsInGeoapifyOrder()
    {
        var handler = HandlerReturning(FeatureCollection());
        var controller = NewController(handler);

        // The Geoapify filter is rect:west,south,east,north
        await controller.GetLocations(58, 7, 59, 9, "hospital", default);

        var url = handler.Requests.Single().RequestUri!.AbsoluteUri;
        Assert.Contains("filter=rect%3A7%2C58%2C9%2C59", url);
    }

    // ---------- Reading the answer ----------

    [Fact]
    public async Task GetLocations_Hospital_IsReturnedWithAllFields()
    {
        var controller = NewController(HandlerReturning(FeatureCollection(Hospital())));

        var result = await Call(controller, "hospital");

        var place = OkBody(result)[0];
        Assert.Equal("Sørlandet sykehus", place.GetProperty("name").GetString());
        Assert.Equal("hospital", place.GetProperty("category").GetString());
        Assert.Equal(58.15, place.GetProperty("latitude").GetDouble(), 4);
        Assert.Equal(8.0, place.GetProperty("longitude").GetDouble(), 4);
        Assert.Equal("Sørlandet sykehus, Kristiansand", place.GetProperty("address").GetString());
    }

    [Fact]
    public async Task GetLocations_PlaceWithoutKnownCategory_IsSkipped()
    {
        var other = Feature("{\"name\":\"Kafe\",\"lat\":58.1,\"lon\":8.1,\"categories\":[\"catering.cafe\"]}");
        var controller = NewController(HandlerReturning(FeatureCollection(other)));

        var result = await Call(controller);

        Assert.Equal(0, OkBody(result).GetArrayLength());
    }

    // ---------- Errors from Geoapify or the network ----------

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GetLocations_GeoapifyErrorStatus_Gives502WithTheStatusCode(HttpStatusCode status)
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("{}", status));
        var controller = NewController(handler);

        var result = await Call(controller);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
        Assert.Equal($"Geoapify returned HTTP {(int)status}.", MessageOf(result));
    }

    [Fact]
    public async Task GetLocations_Timeout_Gives504()
    {
        // The request is cancelled, but NOT by the caller's token. That means a timeout.
        var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException("timeout"));
        var controller = NewController(handler);

        var result = await Call(controller);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(504, objectResult.StatusCode);
        Assert.Equal("Geoapify request timed out.", MessageOf(result));
    }

    [Fact]
    public async Task GetLocations_CancelledByTheCaller_IsNotTurnedIntoA504()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var controller = NewController(HandlerReturning(FeatureCollection()));

        // When the browser closes the connection, the cancellation should simply pass through.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Call(controller, "hospital", source.Token));
    }
}
