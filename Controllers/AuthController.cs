using System.Security.Claims;
using System.Security.Cryptography;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace H_st_2026_Gruppe_1.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model)
    {
        if (model.UserType is not ("ResourceProvider" or "PublicActor"))
        {
            ModelState.AddModelError(nameof(model.UserType), "Please select a valid user.");
            return View(model);
        }

        return View("TwoFactor", new TwoFactorViewModel
        {
            UserType = model.UserType,
            Code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6")
        });
    }

    [HttpGet]
    public IActionResult TwoFactor()
    {
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyTwoFactor(TwoFactorViewModel model)
    {
        if (model.UserType is not ("ResourceProvider" or "PublicActor"))
        {
            return RedirectToAction(nameof(Login));
        }

        var displayName = model.UserType == "PublicActor" ? "Public Actor" : "Resource Provider";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, model.UserType),
            new("UserType", model.UserType)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
