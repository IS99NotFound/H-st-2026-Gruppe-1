using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

// The resources that are registered through the form.
public class ResourceEntry : IValidatableObject
{
	public int Id { get; set; }

	[Required(ErrorMessage = "Full Name is required.")]
	[StringLength(80, MinimumLength = 1, ErrorMessage = "Full Name must be at least {2} and at most {1} characters long.")]
	public string Name { get; set; } = "";

	[Required(ErrorMessage = "Email is required.")]
	[EmailAddress(ErrorMessage = "Please provide a valid email address.")]
	public string Email { get; set; } = "";

	[Required(ErrorMessage = "Phone number is required.")]
	[Phone(ErrorMessage = "Please provide a valid phone number.")]
	public string PhoneNumber { get; set; } = "+47 ";

	[Range(1, int.MaxValue, ErrorMessage = "Select a resource type.")]
	public int CategoryId { get; set; }
	public Category? Category { get; set; }

	// Required by the database. Set in the controller, not in the form.
	public int ProviderId { get; set; }
	public int RegionId { get; set; }

	[Required(ErrorMessage = "Description must be filled out.")]
	public string Description { get; set; } = "";
	
	public DateTime? ExpirationDate { get; set; }
	public DateTime AvailableFrom { get; set; } = DateTime.Now;

	[Required(ErrorMessage = "Position on the map must be filled out")]
	public double? Latitude { get; set; }

	[Required(ErrorMessage = "Position on the map must be filled out.")]
	public double? Longitude { get; set; }

	[StringLength(200)]
	public string LocationName { get; set; } = "";

	public DateTime RegisteredAt { get; set; }
	public string Status { get; set; } = "available";

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (ExpirationDate.HasValue && ExpirationDate < AvailableFrom)
		{
			yield return new ValidationResult(
				"End date cannot be before start date.",
				new[] { nameof(ExpirationDate) });
		}
	}
}

//Attributes to save resources and show them in MyResources
public class ResourceDashboard
{
	public int TotalResources { get; set; }
	public int ExpiringSoonCount { get; set; }
	public int ExpiredCount { get; set; }

	public List<ResourceEntry> Resources { get; set; } = new();
	public string? Search { get; set; }
	public string? Status { get; set; }
	public int? ResourceType { get; set; }
	public DateTime? StartDate { get; set; }
	public DateTime? EndDate { get; set; }
}