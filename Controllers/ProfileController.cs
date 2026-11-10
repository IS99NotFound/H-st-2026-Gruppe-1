using System.Security.Claims;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using AppUserRole = H_st_2026_Gruppe_1.Models.IdentityUserRole;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _db;

    //Handles user accounts, including password changes and account deletion
    private readonly UserManager<User> _userManager;

    //Provides access to application's environment and file storage paths
    private readonly IWebHostEnvironment _environment;

    public ProfileController(
        AppDbContext db,
        UserManager<User> userManager,
        IWebHostEnvironment environment)
    {
        _db = db;
        _userManager = userManager;
        _environment = environment;

    }

    //Shows the profile
    public async Task<IActionResult> Index(bool edit = false)
    {
        //Gets ID to the logged on user
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(id, out int userId))
            return Challenge();

        //Gets user from the database
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        if (user.Role == IdentityUserRole.Administrator)
            return Forbid();

        //Gets information about the user
        var model = new ProfileViewModel
        {
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            ProfileAbout = user.ProfileAbout,
            EmploymentDuration = user.EmploymentDuration,
            IsPublicActor = user.Role == IdentityUserRole.PublicActor,
            ProfileEdit = edit,
            ProfileImagePath = user.ProfileImagePath,
        };

        if (model.IsPublicActor)
        {
            //Gets the organization the user choose at registering
            model.OrganizationName = await (
                from actor in _db.PublicActors
                join organization in _db.Organizations
                    on actor.OrganizationId equals organization.OrganizationId
                where actor.PublicActorId == userId
                select organization.OrganizationName
            ).FirstOrDefaultAsync();
            
            //Counts the users registered resources
            model.NeedCount = await _db.Needs
                .CountAsync(n => n.PublicActorId == userId);
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        string? profileAbout,
        string? employmentDuration)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(id, out int userId))
            return Challenge();

        var user = await _db.Users.FindAsync(userId);

        //Checks if text is too long
        if (profileAbout?.Length > 500 ||
            employmentDuration?.Length > 100)
        {
            TempData["Error"] = "Teksten er for lang.";

            return RedirectToAction (
                nameof(Index),
                new {edit = true});
        }

        //Civilians(resource providers) kan only edit "Om meg"
        if (user.Role == IdentityUserRole.CivilianProvider)
        {
            user.ProfileAbout = string.IsNullOrWhiteSpace(profileAbout)
                ? null : profileAbout.Trim();
        }

        //Public Actors can only edit their how long their employment is/was
        else if (user.Role == IdentityUserRole.PublicActor)
        {
            user.EmploymentDuration =
                string.IsNullOrWhiteSpace(employmentDuration)
                ? null : employmentDuration.Trim();
        }
        else
        {
            return Forbid();
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Profilen ble oppdatert!";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> UploadProfilePicture(IFormFile? profilePicture)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        //Checks if a file was selected
        if (profilePicture == null || profilePicture.Length == 0)
        {
            TempData["Error"] = "Velg et bilde først.";
            return RedirectToAction(nameof(Index));
        }

        //Maximum file size: 2MB
        if (profilePicture.Length > 2 * 1024 *1024)
        {
            TempData["Error"] = "Bildet kan ikke være større enn 2 MB.";
            return RedirectToAction(nameof(Index));
        }

        //Allow only common image formats
        var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();

        if (extension != ".jpg" &&
            extension != ".jpeg" &&
            extension != ".png" &&
            extension != ".webp")
        {
            TempData["Error"] = "Ugyldig bildefil.";
            return RedirectToAction(nameof(Index));
        }

        //Create the image path
        var folder = Path.Combine(_environment.WebRootPath, "profile-images");

        var fileName = $"user_{user.Id}{extension}";
        var filePath = Path.Combine(folder, fileName);

        //Save the new image
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await profilePicture.CopyToAsync(stream);
        }

        //Update the image path in the database
        var oldImage = user.ProfileImagePath;
        user.ProfileImagePath = "/profile-images/" + fileName;

        await _db.SaveChangesAsync();

        //Remove previous image if the format changed
        if (!string.IsNullOrEmpty(oldImage) &&
            oldImage != user.ProfileImagePath)
        {
            var oldFile = Path.Combine(folder, Path.GetFileName(oldImage));
            //Checks if the ol
            if (System.IO.File.Exists(oldFile))
                System.IO.File.Delete(oldFile);
        }
         TempData["Success"] = "Profilbildet ble oppdatert!";

    return RedirectToAction(nameof(Index));

    }

    public async Task<IActionResult> ChangePassword(
        string currentPassword,
        string newPassword,
        string confirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["PasswordError"] = "Fyll ut alle passordfeltene.";
            return RedirectToAction(nameof(Index));
        }

        if (newPassword != confirmPassword)
        {
            TempData["PasswordError"] = "De nye passordene er ikke like.";
            return RedirectToAction(nameof(Index));
        }

        //Identity checks the current password and saves the new password hash
        var result = await _userManager.ChangePasswordAsync(
            user,
            currentPassword,
            newPassword
        );

        if (!result.Succeeded)
        {
            TempData["PasswordError"] =
            "Kunne ikke endre passord. Kontroller nåværende passord og kravene til det nye passordet.";

            return RedirectToAction(nameof(Index));
        }

        //Log out after changing password
        await HttpsContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        return RedirectToAction("Login", "Auth");
    }   

    public async Task<IActionResult> DeleteAccount(
        string currentPassword,
        string confirmationText)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        //Administrators cannot delete their own accounts
        if (user.Role == AppUserRole.Administrator)
            return Forbid();

        //Require password an confirmation
    }
}