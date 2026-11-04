using System.ComponentModel.DataAnnotations;
namespace H_st_2026_Gruppe_1.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Brukernavn er påkrevd.")]
    public string UserName { get; set; } = "";
    
    [Required(ErrorMessage = "Passord er påkrevd.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class TwoFactorViewModel
{
    [Required]
    public string PendingId { get; set; } = "";
    
    [Required(ErrorMessage = "Kode er påkrevd.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Koden må inneholde 6 siffer.")]
    public string Code { get; set; } = "";
    
    // skrevet midlertidig byttes ut med sms/mail 
    public string? DemoCode { get; set; } = "";
}
