using Microsoft.AspNetCore.Identity;

namespace H_st_2026_Gruppe_1.Models;

// User table. IDentity columns comes from IdentityUser
public class User : IdentityUser<int>
    {
    public string Name { get; set; } = "";
    public IdentityUserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    //Ekstra informasjon som trengs for profilsiden
    public string? ProfileAbout {get; set;}
    public string? EmploymentDuration {get; set;}
    public string? ProfileImagePath {get; set;}
    }

public enum IdentityUserRole
{
    PublicActor,
    CivilianProvider,
    Administrator
}
