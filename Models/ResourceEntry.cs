namespace H_st_2026_Gruppe_1.Models;

// Ressursen som blir registrert via skjemaet
public class ResourceEntry
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Phone { get; set; } = "";
	public string ResourceType { get; set; } = "";
	public string Description { get; set; } = "";

	// Spørsmålstegnet betyr feltet kan være tomt (null), siden man ikke må klikke i kartet
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }

	public DateTime RegisteredAt { get; set; }
}