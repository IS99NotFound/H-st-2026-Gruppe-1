namespace H_st_2026_Gruppe_1.Helpers;

public static class ResourceCategoryPolicy
{
    private static readonly string[] ExcludedCategoryNames =
    [
        "Droneobservasjon",
        "Evakuering"
    ];

    public static bool IsAllowed(string categoryName)
    {
        return !ExcludedCategoryNames.Contains(
            categoryName,
            StringComparer.OrdinalIgnoreCase);
    }
}
