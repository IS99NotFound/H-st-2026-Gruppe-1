using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

// The resources that are registered through the form.
public class ResourceEntry
{
	public int Id { get; set; }
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
	public double? Longitude { get; set; }

	public DateTime RegisteredAt { get; set; }
}