using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

// Forms model 
// Only contains fields that the user fills out + validation rules 
public class ResourceViewModel
{
    // Name required; Max 80 characters 
    [Required(ErrorMessage = "Fullt navn er påkrevd.")]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "Fullt navn må være minst {2} og maks {1} tegn langt.")]
    public string Name { get; set; } = "";

    // Contact information be E-mail OR phone number 
    [Required(ErrorMessage = "Kontaktinformasjon er påkrevd.")]
    [RegularExpression(
        @"^([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}|(\+?[0-9\s]{8,15}))$",
        ErrorMessage = "Oppgi en gyldig e-postadresse eller et gyldig telefonnummer (minst 8 siffer)."
    )]
    public string Phone { get; set; } = "";

    // Description is required 
    [Required(ErrorMessage = "Beskrivelse må fylles ut")]
    public string Description { get; set; } = "";

    // Resource type is required for user
    [Required(ErrorMessage = "Velg en ressurstype.")]
    public string ResourceType { get; set; } = "";

    // Expiration date is optional, calculated by HomeController based on resource type 
    public DateTime? ExpiryDate { get; set; }

    // Latitide is required
    [Required(ErrorMessage = "Posisjon på kartet må fylles ut")]
    public double? Latitude { get; set; }

    // Longitude is required
    [Required(ErrorMessage = "Posisjon på kartet må fylles ut")]
    public double? Longitude { get; set; }
}