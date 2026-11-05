using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Controllers;

[AllowAnonymous]
public class RegisterController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;


    public RegisterController(UserManager<User> userManager, AppDbContext db, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await AddOrganizationsAsync(new RegisterViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegisterViewModel model)
    {
        if (!Enum.IsDefined(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Velg en gyldig rolle.");
        }

        if (model.Role == IdentityUserRole.PublicActor &&
            !await _db.Organizations.AnyAsync(organization => organization.OrganizationId == model.OrganizationId))
        {
            ModelState.AddModelError(nameof(model.OrganizationId), "Velg en organisasjon for offentlige aktører.");
        }

        // Every role can be chosen for now
        if (!_env.IsDevelopment() && model.Role != IdentityUserRole.CivilianProvider)
        {
            ModelState.AddModelError(nameof(model.Role), "Bare ressursleverandører kan registrere seg.");
        }

        if (!ModelState.IsValid)
        {
            return View(await AddOrganizationsAsync(model));
        }

        var user = new User
        {
            UserName = model.UserName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Name = DisplayNameFormatter.Format(model.Name),
            Role = model.Role
        };

        // The User row and the role row are saved, or none of them
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Code switch
                {
                    "PasswordRequiresDigit" => "Passordet må inneholde minst ett tall.",
                    "PasswordRequiresLower" => "Passordet må inneholde minst én liten bokstav.",
                    "PasswordRequiresUpper" => "Passordet må inneholde minst én stor bokstav.",
                    "PasswordRequiresNonAlphanumeric" => "Passordet må inneholde minst ett spesialtegn.",
                    "PasswordTooShort" => "Passordet må være minst 8 tegn.",
                    "DuplicateUserName" => "Brukernavnet er allerede i bruk.",
                    "DuplicateEmail" => "E-postadressen er allerede i bruk.",
                    "InvalidEmail" => "Oppgi en gyldig e-postadresse.",
                    "InvalidUserName" => "Brukernavnet inneholder ugyldige tegn.",
                    _ => error.Description
                });
            }
            return View(await AddOrganizationsAsync(model));
        }

        switch (model.Role)
        {
            case IdentityUserRole.CivilianProvider:
                _db.CivilianProviders.Add(new CivilianProvider
                {
                    ProviderId = user.Id,
                    ProviderType = "private_person"
                });
                break;

            case IdentityUserRole.PublicActor:
                _db.PublicActors.Add(new PublicActor
                {
                    PublicActorId = user.Id,
                    OrganizationId = model.OrganizationId
                });
                break;

                // Administrator only needs the User row
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["RegisterMessage"] = "Kontoen din er opprettet.";
        return RedirectToAction("Login", "Auth");
    }

    private async Task<RegisterViewModel> AddOrganizationsAsync(RegisterViewModel model)
    {
        model.Organizations = await _db.Organizations
            .AsNoTracking()
            .OrderBy(organization => organization.OrganizationName)
            .ToListAsync();

        return model;
    }
}