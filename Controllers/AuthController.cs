using System.Security.Claims;
using System.Security.Cryptography;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace H_st_2026_Gruppe_1.Controllers;

[AllowAnonymous]
public class AuthController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly IMemoryCache _memoryCache;

    public AuthController(UserManager<User> userManager, IMemoryCache memoryCache)
    {
        _userManager = userManager;
        _memoryCache = memoryCache;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("PublicActor"))
            {
                return RedirectToAction("DashboardIndex", "PublicActor");
            }

            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    // Step 1 of login: check username and password, then create the 2FA code.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 1. Søk etter bruker i databasen
        var user = await _userManager.FindByNameAsync(model.UserName);
        
        // 2. Sjekk lockout og passord (Bruker samme generiske feilmelding for å unngå brukernavn-enumeration)
        // Feil passord telles, og kontoen låses etter MaxFailedAccessAttempts (se Program.cs)
        if (user == null || await _userManager.IsLockedOutAsync(user))
        {
            ModelState.AddModelError(string.Empty, "Ugyldig brukernavn eller passord.");
            return View(model);
        }

        if (!await _userManager.CheckPasswordAsync(user, model.Password))
        {
            await _userManager.AccessFailedAsync(user);
            ModelState.AddModelError(string.Empty, "Ugyldig brukernavn eller passord.");
            return View(model);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        // 3. Generer 6-sifret tofakto-kode (simulert SMS)
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var pendingId = Guid.NewGuid().ToString();

        // 4. Lagre i IMemoryCache (Utløper etter 5 minutter, maks 3 forsøk logikk kan styres her)
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
        };
        
        var pendingData = new PendingTwoFactorData
        {
            UserId = user.Id,
            Code = code,
            Attempts = 0
        };

        _memoryCache.Set(pendingId, pendingData, cacheEntryOptions);

        // 5. Send til TwoFactor-visning (DemoCode viser koden for enklere testing)
        return View("TwoFactor", new TwoFactorViewModel
        {
            PendingId = pendingId,
            DemoCode = code // Fjern eller kommenter ut denne i ekte produksjon
        });
    }

    // The 2FA page cannot be opened directly, so we send the user back to the login page.
    [HttpGet]
    public IActionResult TwoFactor()
    {
        return RedirectToAction(nameof(Login));
    }

    // Step 2 of login: check the 2FA code. If it is correct, the user is signed in.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyTwoFactor(TwoFactorViewModel model)
    {
        if (string.IsNullOrEmpty(model.PendingId) || string.IsNullOrEmpty(model.Code))
        {
            ModelState.AddModelError(string.Empty, "Ugyldig forespørsel.");
            return RedirectToAction(nameof(Login));
        }

        // Hent fra cache
        if (!_memoryCache.TryGetValue(model.PendingId, out PendingTwoFactorData? pending) || pending == null)
        {
            ModelState.AddModelError(string.Empty, "Tofaktorkoden har utløpt. Logg inn på nytt.");
            return RedirectToAction(nameof(Login));
        }

        // Sjekk antall forsøk (maks 3)
        pending.Attempts++;
        if (pending.Attempts > 3)
        {
            _memoryCache.Remove(model.PendingId);
            ModelState.AddModelError(string.Empty, "For mange feilforsøk. Logg inn på nytt.");
            return RedirectToAction(nameof(Login));
        }

        // Valider koden sikkert mot tidsangrep (FixedTimeEquals)
        var codeBytesProvided = System.Text.Encoding.UTF8.GetBytes(model.Code);
        var codeBytesExpected = System.Text.Encoding.UTF8.GetBytes(pending.Code);
        var codeMatches = CryptographicOperations.FixedTimeEquals(
            codeBytesProvided,
            codeBytesExpected);

        if (!codeMatches)
        {
            _memoryCache.Set(model.PendingId, pending); // Oppdater forsøk
            ModelState.AddModelError(nameof(model.Code), "Feil kode. Prøv igjen.");
            model.DemoCode = pending.Code; // Behold demo-koden på skjermen slik du planla!
            return View("TwoFactor", model);
        }

        // Fjern koden fra cachen slik at den kun kan brukes én gang
        _memoryCache.Remove(model.PendingId);

        // Hent brukeren fra databasen basert på ID lagret i cachen
        var user = await _userManager.FindByIdAsync(pending.UserId.ToString());
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Bygg sikre claims fra databasen
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name ?? user.UserName ?? "Bruker"),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("UserType", user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // Sikrere levetid enn 365 dager
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        // Public actors start on their dashboard. Everyone else starts on the home page.
        if (user.Role.ToString() == "PublicActor")
        {
            return RedirectToAction("DashboardIndex", "PublicActor");
        }

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

// Hjelpeklasse for cachen
public class PendingTwoFactorData
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Attempts { get; set; }
}