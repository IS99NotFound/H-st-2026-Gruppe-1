using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Services;

public sealed record LocationRegionResult(bool IsValid, int? RegionId, string? ErrorMessage)
{
    public static LocationRegionResult Invalid(string message) => new(false, null, message);
    public static LocationRegionResult Valid(int regionId) => new(true, regionId, null);
}

public sealed class LocationRegionResolver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _db;

    public LocationRegionResolver(IHttpClientFactory httpClientFactory, AppDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _db = db;
    }

    public async Task<LocationRegionResult> ResolveAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (latitude is < 57.5 or > 71.5 || longitude is < 4 or > 32)
        {
            return LocationRegionResult.Invalid("Lokasjonen må være i Norge.");
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("ResQ", "1.0"));

        var url = "https://nominatim.openstreetmap.org/reverse" +
            $"?format=jsonv2&lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}&zoom=10&addressdetails=1" +
            "&accept-language=no";

        JsonDocument response;
        try
        {
            response = await client.GetFromJsonAsync<JsonDocument>(url, cancellationToken)
                ?? throw new HttpRequestException("Tomt svar fra geokodingstjenesten.");
        }
        catch (HttpRequestException)
        {
            return LocationRegionResult.Invalid(
                "Kunne ikke kontrollere lokasjonen akkurat nå. Prøv igjen.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return LocationRegionResult.Invalid(
                "Kontroll av lokasjonen tok for lang tid. Prøv igjen.");
        }

        using (response)
        {
            var address = response.RootElement.TryGetProperty("address", out var addressElement)
                ? addressElement
                : default;

            var countryCode = GetAddressValue(address, "country_code");
            if (!string.Equals(countryCode, "no", StringComparison.OrdinalIgnoreCase))
            {
                return LocationRegionResult.Invalid("Lokasjonen må være i Norge.");
            }

            var county = GetAddressValue(address, "state") ??
                GetAddressValue(address, "county");
            var countyCode = GetAddressValue(address, "ISO3166-2-lvl4");
            var regionName = RegionNameFromCode(countyCode) ??
                NormalizeRegionName(county);
            if (string.IsNullOrWhiteSpace(regionName))
            {
                return LocationRegionResult.Invalid(
                    "Fant ikke fylke for lokasjonen. Velg en annen posisjon.");
            }

            var regionId = await _db.Set<Region>()
                .Where(region => region.RegionName == regionName)
                .Select(region => (int?)region.RegionId)
                .SingleOrDefaultAsync(cancellationToken);

            return regionId.HasValue
                ? LocationRegionResult.Valid(regionId.Value)
                : LocationRegionResult.Invalid(
                    $"Fylket «{regionName}» finnes ikke i regionregisteret.");
        }
    }

    public LocationRegionResult Resolve(double latitude, double longitude)
    {
        return ResolveAsync(latitude, longitude).GetAwaiter().GetResult();
    }

    private static string? RegionNameFromCode(string? code)
    {
        return code?.ToUpperInvariant() switch
        {
            "NO-01" => "Østfold",
            "NO-02" => "Akershus",
            "NO-03" => "Oslo",
            "NO-34" => "Innlandet",
            "NO-33" => "Buskerud",
            "NO-39" => "Vestfold",
            "NO-40" => "Telemark",
            "NO-42" => "Agder",
            "NO-11" => "Rogaland",
            "NO-46" => "Vestland",
            "NO-15" => "Møre og Romsdal",
            "NO-50" => "Trøndelag",
            "NO-18" => "Nordland",
            "NO-55" => "Troms",
            "NO-56" => "Finnmark",
            _ => null
        };
    }

    private static string? NormalizeRegionName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return name
            .Replace(" fylke", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" kommune", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static string? GetAddressValue(JsonElement address, string propertyName)
    {
        return address.ValueKind == JsonValueKind.Object &&
            address.TryGetProperty(propertyName, out var value)
                ? value.GetString()
                : null;
    }
}
