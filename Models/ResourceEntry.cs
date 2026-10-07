using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

// The resources that are registered through the form.
public class ResourceEntry
{
	public int Id { get; set; }

	[Required(ErrorMessage = "Full Name is required.")]
	[StringLength(80, MinimumLength = 1, ErrorMessage = "Full Name must be at least {2} and at most {1} characters long.")]
	public string Name { get; set; } = "";

	[Required(ErrorMessage = "Contact information is required.")]
	[RegularExpression(
		@"^([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}|(\+?[0-9\s]{8,15}))$",
		ErrorMessage = "Please provide a valid email address or a valid phone number (at least 8 digits)."
	)]
	public string Phone { get; set; } = "";

	[Required(ErrorMessage = "Select a resource type.")]
	public string ResourceType { get; set; } = "";

	[Required(ErrorMessage = "Description must be filled out.")]
	public string Description { get; set; } = "";
	
	[Required(ErrorMessage = "Expiry date is required.")]
	public DateTime? ExpirationDate { get; set; }

	[Required(ErrorMessage = "Position on the map must be filled out")]
	public double? Latitude { get; set; }

	[Required(ErrorMessage = "Position on the map must be filled out.")]
	public double? Longitude { get; set; }

	public DateTime RegisteredAt { get; set; }
}

//attributter for å lagre resources og presentere de i dashboad view i MyResources
public class ResourceDashboard
{
	public int TotalResources { get; set; }
	public int ExpiringSoonCount { get; set; }
	public int ExpiredCount { get; set; }

	public List<ResourceEntry> Resources { get; set; } = new();
}