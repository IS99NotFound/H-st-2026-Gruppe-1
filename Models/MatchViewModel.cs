namespace H_st_2026_Gruppe_1.Models;

// One matching resource together with its distance from the need.
public class ResourceMatch
{
	public ResourceEntry Resource { get; set; } = new();
	public double DistanceKm { get; set; }
}

// Data sent to the Matches view: the need, the chosen radius and the resources found.
public class MatchViewModel
{
	public ResourceNeed Need { get; set; } = new();
	public double RadiusKm { get; set; }
	public bool AllCategories { get; set; }
	public List<ResourceMatch> Matches { get; set; } = new();
}
