namespace H_st_2026_Gruppe_1.Models;

// Ressursen slik den lagres i minnet (ResourceStore).
// Valideringen ligger i ResourceViewModel, ikke her.
public class ResourceEntry
{
    // Settes automatisk av ResourceStore.Add
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Description { get; set; } = "";
    public string ResourceType { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }

    // Spørsmålstegn betyr at feltet kan være tomt (null)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Settes automatisk av ResourceStore.Add
    public DateTime RegisteredAt { get; set; }
}