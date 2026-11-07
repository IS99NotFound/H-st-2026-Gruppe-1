namespace H_st_2026_Gruppe_1.Helpers;

public static class CategoryLabelHelper
{
    private static readonly IReadOnlyDictionary<string, string> NorwegianLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Communications"] = "Samband",
            ["Crew / personnel"] = "Mannskap",
            ["Heavy machinery"] = "Maskiner",
            ["Power / generator"] = "Strøm/aggregat",
            ["Snow clearing"] = "Snørydding",
            ["Sand / gravel"] = "Sand/grus",
            ["Premises"] = "Lokaler",
            ["Boat"] = "Båt",
            ["Fuel"] = "Drivstoff",
            ["Other"] = "Annet"
        };

    public static string DisplayName(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return "";
        }

        return NorwegianLabels.TryGetValue(categoryName, out var norwegianName)
            ? norwegianName
            : categoryName;
    }
}
