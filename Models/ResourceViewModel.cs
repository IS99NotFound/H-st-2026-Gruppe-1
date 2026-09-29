using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

// Modellen som skjemaet bruker.
// Den har kun feltene brukeren fyller ut, pluss valideringsreglene.
public class ResourceViewModel
{
    // Navn er påkrevd, maks 80 tegn
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "Full Name must be at least {2} and at most {1} characters long.")]
    public string Name { get; set; } = "";

    // Kontaktinfo kan være e-post ELLER telefonnummer
    [Required(ErrorMessage = "Contact information is required.")]
    [RegularExpression(
        @"^([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}|(\+?[0-9\s]{8,15}))$",
        ErrorMessage = "Please provide a valid email address or a valid phone number (at least 8 digits)."
    )]
    public string Phone { get; set; } = "";

    // Beskrivelse må fylles ut
    [Required(ErrorMessage = "Description must be filled out")]
    public string Description { get; set; } = "";

    // Brukeren må velge en ressurstype
    [Required(ErrorMessage = "Select a resource type.")]
    public string ResourceType { get; set; } = "";

    // Utløpsdato er valgfri
    public DateTime? ExpiryDate { get; set; }

    // Latitude er valgfri i seg selv
    public double? Latitude { get; set; }

    // Longitude er påkrevd, så brukeren må klikke i kartet
    [Required(ErrorMessage = "Position on the map must be filled out")]
    public double? Longitude { get; set; }
}