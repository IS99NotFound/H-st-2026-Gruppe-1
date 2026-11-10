namespace H_st_2026_Gruppe_1.Models;

public class ProfileViewModel
{
    //Information from existing User-tabel
    public string Name {get; set;} = "";
    public string? Email {get; set;}
    public string? PhoneNumber {get; set;}
    public DateTime CreatedAt {get; set;}

    public bool IsPublicActor {get; set;}

    public string? OrganizationName {get; set;}

    //Extra profileinformation
    public string? ProfileAbout {get; set;}
    public string? EmploymentDuration {get; set;}

    //Statistic
    public int ResourceCount {get; set;}
    public int NeedCount {get; set;}

    public bool ProfileEdit {get; set;}
    public string? ProfileImagePath { get; set; }

}