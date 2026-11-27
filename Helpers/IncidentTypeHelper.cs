namespace H_st_2026_Gruppe_1.Helpers;

public static class IncidentTypeHelper
{
    public static string DisplayName(string? type) => type switch
    {
        "Forest fire" => "Skogbrann",
        "Landslide" => "Jordskred",
        "Flood" => "Flom",
        "Storm" => "Storm",
        "Power outage" => "Strømbrudd",
        "Other" => "Annet",
        _ => type ?? ""
    };
}
