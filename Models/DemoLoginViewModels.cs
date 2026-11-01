using System.ComponentModel.DataAnnotations;
namespace H_st_2026_Gruppe_1.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string UserName { get; set; } = "";
    
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class TwoFactorViewModel
{
    [Required]
    public string PendingId { get; set; } = "";
    
    [StringLength(6, MinimumLength = 6, ErrorMessage = "The code has 6 digits.")]
    public string Code { get; set; } = "";
    
    // skrevet midlertidig byttes ut med sms/mail 
    public string? DemoCode { get; set; } = "";
}
