namespace H_st_2026_Gruppe_1.Models;

public class LoginViewModel
{
    public string UserType { get; set; } = "ResourceProvider";
}

public class TwoFactorViewModel
{
    public string UserType { get; set; } = "ResourceProvider";
    public string Code { get; set; } = string.Empty;
}
