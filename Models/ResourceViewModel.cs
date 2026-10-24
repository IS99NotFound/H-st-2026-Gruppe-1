using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

// Forms model 
// Only contains fields that the user fills out + validation rules 
public class ResourceViewModel
{
    // Name required; Max 80 characters 
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "Full Name must be at least {2} and at most {1} characters long.")]
    public string Name { get; set; } = "";

    // Contact information be E-mail OR phone number 
    [Required(ErrorMessage = "Contact information is required.")]
    [RegularExpression(
        @"^([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}|(\+?[0-9\s]{8,15}))$",
        ErrorMessage = "Please provide a valid email address or a valid phone number (at least 8 digits)."
    )]
    public string Phone { get; set; } = "";

    // Description is required 
    [Required(ErrorMessage = "Description must be filled out")]
    public string Description { get; set; } = "";

    // Resource type is required for user
    [Required(ErrorMessage = "Select a resource type.")]
    public string ResourceType { get; set; } = "";

    // Expiration date is optional, calculated by HomeController based on resource type 
    public DateTime? ExpiryDate { get; set; }

    // Latitide is required
    [Required(ErrorMessage = "Position on the map must be filled out")]
    public double? Latitude { get; set; }

    // Longitude is required
    [Required(ErrorMessage = "Position on the map must be filled out")]
    public double? Longitude { get; set; }
}