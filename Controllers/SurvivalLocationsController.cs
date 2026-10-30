
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace H_st_2026_Gruppe_1.Controllers;

[ApiController]
[Route("api/survival-locations")]
public class SurvivalLocationsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SurvivalLocationsController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetLocations(
        [FromQuery] double south,
        [FromQuery] double west,
        [FromQuery] double north,
        [FromQuery] double east,
        [FromQuery] string categories = "hospital,drinking_water",
        CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(south) ||
            !double.IsFinite(west) ||
            !double.IsFinite(north) ||
            !double.IsFinite(east) ||
            south < -90 || north > 90 ||
            west < -180 || east > 180 ||
            south >= north || west >= east)
        {
            return BadRequest(new { message = "Invalid map bounds." });
        }

        var categoryMap = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["hospital"] = "healthcare.hospital",
            ["drinking_water"] = "amenity.drinking_water"
        };

        var selectedCategories = categories
            .Split(',', StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedCategories.Length == 0 ||
            selectedCategories.Any(c => !categoryMap.ContainsKey(c)))
        {
            return BadRequest(new
            {
                message = "Select hospital or drinking_water."
            });
        }

        var apiKey = _configuration["Geoapify:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return StatusCode(500, new
            {
                message = "Geoapify API key is not configured."
            });
        }

        var geoCategories = string.Join(",",
            selectedCategories.Select(c => categoryMap[c]));

        var filter = $"rect:{west},{south},{east},{north}";

        var url =
            "https://api.geoapify.com/v2/places" +
            $"?categories={Uri.EscapeDataString(geoCategories)}" +
            $"&filter={Uri.EscapeDataString(filter)}" +
            "&limit=200&lang=en" +
            $"&apiKey={Uri.EscapeDataString(apiKey)}";

        try
        {
            var client = _httpClientFactory.CreateClient();

            using var response = await client.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(502, new
                {
                    message = $"Geoapify returned HTTP {(int)response.StatusCode}."
                });
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            using var document = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty(
                    "features", out var features) ||
                features.ValueKind != JsonValueKind.Array)
            {
                return StatusCode(502, new
                {
                    message = "Unexpected response from Geoapify."
                });
            }

            var locations = new List<object>();

            foreach (var feature in features.EnumerateArray())
            {
                if (!feature.TryGetProperty(
                        "properties", out var properties))
                {
                    continue;
                }

                var geoCategoriesForPlace = new List<string>();

                if (properties.TryGetProperty(
                        "categories", out var categoryArray) &&
                    categoryArray.ValueKind == JsonValueKind.Array)
                {
                    geoCategoriesForPlace = categoryArray
                        .EnumerateArray()
                        .Where(c => c.ValueKind == JsonValueKind.String)
                        .Select(c => c.GetString() ?? "")
                        .ToList();
                }

                string? category = null;

                if (selectedCategories.Contains(
                        "hospital", StringComparer.OrdinalIgnoreCase) &&
                    geoCategoriesForPlace.Contains(
                        "healthcare.hospital"))
                {
                    category = "hospital";
                }
                else if (selectedCategories.Contains(
                             "drinking_water",
                             StringComparer.OrdinalIgnoreCase) &&
                         geoCategoriesForPlace.Contains(
                             "amenity.drinking_water"))
                {
                    category = "drinking_water";
                }

                if (category == null)
                {
                    continue;
                }

                double? latitude = ReadNumber(properties, "lat");
                double? longitude = ReadNumber(properties, "lon");

                if (latitude == null || longitude == null)
                {
                    continue;
                }

                locations.Add(new
                {
                    name = ReadString(properties, "name")
                        ?? ReadString(properties, "address_line1")
                        ?? "Unnamed location",
                    category,
                    latitude,
                    longitude,
                    address = ReadString(properties, "formatted")
                });
            }

            return Ok(locations);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                message = "Geoapify request timed out."
            });
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new
            {
                message = "Could not contact Geoapify."
            });
        }
        catch (JsonException)
        {
            return StatusCode(502, new
            {
                message = "Could not read the Geoapify response."
            });
        }
    }

    private static string? ReadString(
        JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static double? ReadNumber(
        JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) &&
               value.TryGetDouble(out var number) &&
               double.IsFinite(number)
            ? number
            : null;
    }
}
