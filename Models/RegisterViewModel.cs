using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

//Registration form fields
public class RegisterViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = " ";

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 2)]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email adress.")]
    [StringLength(50, MinimumLength = 5)]
    public string Email { get; set; } = "";

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Select a role.")]
    public IdentityUserRole Role { get; set; } = IdentityUserRole.CivilianProvider;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
           
}
