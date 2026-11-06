namespace H_st_2026_Gruppe_1.Models;
    // A user who offers resources (a civilian provider). Saved in the CivilianProvider table.
    public class CivilianProvider
    {
    // Same number as the user id.
    public int ProviderId { get; set; }
    public string ProviderType { get; set; } = "Civilian";
    }
