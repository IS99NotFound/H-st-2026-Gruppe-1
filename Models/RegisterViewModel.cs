using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

//Registration form fields
public class RegisterViewModel
{
    [Required(ErrorMessage = "Navn er påkrevd.")]
    [StringLength(100, ErrorMessage = "Navn kan ikke være lengre enn 100 tegn.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Brukernavn er påkrevd.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Brukernavn må være mellom 2 og 50 tegn.")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "E-post er påkrevd.")]
    [EmailAddress(ErrorMessage = "Oppgi en gyldig e-postadresse.")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "E-post må være mellom 5 og 50 tegn.")]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "Oppgi et gyldig telefonnummer.")]
    [StringLength(20, ErrorMessage = "Telefonnummer kan ikke være lengre enn 20 tegn.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Velg en rolle.")]
    public IdentityUserRole Role { get; set; } = IdentityUserRole.CivilianProvider;

    public int OrganizationId { get; set; }

    public IReadOnlyList<Organization> Organizations { get; set; } = [];

    [Required(ErrorMessage = "Passord er påkrevd.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Passord må være mellom 8 og 100 tegn.")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passordene stemmer ikke overens.")]
    public string ConfirmPassword { get; set; } = "";
           
}
