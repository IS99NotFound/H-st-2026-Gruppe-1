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

	[Required(ErrorMessage = "Description must be filled out")]
	public string Description { get; set; } = "";
	public string ResourceType { get; set; } = "";

	// Spørsmålstegnet betyr feltet kan være tomt (null), siden man ikke må klikke i kartet
	[Required(ErrorMessage = "Position on the map must be filled out")]
	[Required(ErrorMessage = "Name is required.")]
	public string Name { get; set; } = "";
	// Contact information is required, but can be a phone number or email address.
	[Required(ErrorMessage = "A phone number or email address is required.")]
	public string Phone { get; set; } = "";
	[Required(ErrorMessage = "Select a resource type.")]
	public string ResourceType { get; set; } = "";
	public string Description { get; set; } = "";
	public DateTime? ExpiryDate { get; set; }

	// The question mark means the field can be empty, since selecting a map position is optional.
	public double? Latitude { get; set; }
	[Required(ErrorMessage = "Position on the map must be filled out")]
	public double? Longitude { get; set; }
	public DateTime RegisteredAt { get; set; }
}