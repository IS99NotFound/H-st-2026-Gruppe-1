namespace H_st_2026_Gruppe_1.Models;

public static class SurvialLocationsStore
{
    public static List<SurvivalLocation> Locations {get; } = new();

    public static readonly string[] Categories =
    {
        "Hospital",
        "Drinking Water",
        "Escape Bunker",
        "Shelter",
        "Food",
        "Medical"
    };
}