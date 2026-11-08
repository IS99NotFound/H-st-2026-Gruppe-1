namespace H_st_2026_Gruppe_1.Helpers;

public static class StatusLabelHelper
{
    public static string DisplayName(string? status) => status switch
    {
        "active" => "Aktiv",
        "new" => "Ny",
        "under_review" or "proposed" => "Vurderes",
        "assigned" or "accepted" => "Tildelt",
        "resolved" => "Løst",
        "cancelled" => "Kansellert",
        "rejected" or "declined" => "Avslått",
        "completed" => "Fullført",
        "available" => "Tilgjengelig",
        "unavailable" => "Utilgjengelig",
        "withdrawn" => "Trukket tilbake",
        _ => status ?? ""
    };
}
