using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

// The resources that are registered through the form.
public class ResourceEntry : IValidatableObject
{
	
	public int Id { get; set; }

	[Required(ErrorMessage = "Fullt navn er påkrevd.")]
	[StringLength(80, MinimumLength = 1, ErrorMessage = "Fullt navn må være minst {2} og maks {1} tegn langt.")]
	public string Name { get; set; } = "";

	[Required(ErrorMessage = "E-post er påkrevd.")]
	[EmailAddress(ErrorMessage = "Oppgi en gyldig e-postadresse.")]
	public string Email { get; set; } = "";

	[Required(ErrorMessage = "Telefonnummer er påkrevd.")]
	[Phone(ErrorMessage = "Oppgi et gyldig telefonnummer.")]
	public string PhoneNumber { get; set; } = "+47 ";

	[Range(1, int.MaxValue, ErrorMessage = "Velg en ressurstype.")]
	public int CategoryId { get; set; }
	public Category? Category { get; set; }

	// Required by the database. Set in the controller, not in the form.
	public int ProviderId { get; set; }
	public int RegionId { get; set; }

	[Required(ErrorMessage = "Beskrivelse må fylles ut.")]
	public string Description { get; set; } = "";
	
	public DateTime? ExpirationDate { get; set; }
	public DateTime AvailableFrom { get; set; } = DateTime.Now;

	[Required(ErrorMessage = "Posisjon på kartet må fylles ut")]
	public double? Latitude { get; set; }

	[Required(ErrorMessage = "Posisjon på kartet må fylles ut.")]
	public double? Longitude { get; set; }

	[Required(ErrorMessage = "Stedsnavn er påkrevd.")]
	[StringLength(200, ErrorMessage = "Stedsnavn kan ikke være lengre enn 200 tegn.")]
	public string LocationName { get; set; } = "";

	public DateTime RegisteredAt { get; set; }
	public string Status { get; set; } = "available";

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (ExpirationDate.HasValue && ExpirationDate < AvailableFrom)
		{
			yield return new ValidationResult(
				"Sluttdato kan ikke være før startdato.",
				new[] { nameof(ExpirationDate) });
		}
	}
}

//Attributes to save resources and show them in MyResources
public class ResourceDashboard
{
	public List<ResourceMatchStatusViewModel> MatchStatuses { get; set; } = new();
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