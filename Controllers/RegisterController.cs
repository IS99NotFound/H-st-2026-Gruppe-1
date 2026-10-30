using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
    public IActionResult Index()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegisterViewModel model)
    {
        if (!Enum.IsDefined(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Select a valid role.");
        }

        // Every role can be chosen for now
        if (!_env.IsDevelopment() && model.Role != IdentityUserRole.CivilianProvider)
        {
            ModelState.AddModelError(nameof(model.Role), "Only resource providers can register.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new User
        {
            UserName = model.UserName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Name = model.Name,
            Role = model.Role
        };

        // The User row and the role row are saved, or none of them
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
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
                    OrganizationId = DemoDefaults.OrganizationId
                });
                break;

                // Administrator only needs the User row
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["RegisterMessage"] = "Your account has been created successfully.";
        return RedirectToAction("Login", "Auth");
    }
}