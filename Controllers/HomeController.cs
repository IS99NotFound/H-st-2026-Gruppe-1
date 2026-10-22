using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace H_st_2026_Gruppe_1.Controllers;

public class PublicActorController : Controller
{
    public IActionResult DashboardIndex()
    {
        var model = new ResourceNeed();
        return View("DashboardIndex", model);
    }
    
    
    [HttpPost]
    public IActionResult RegisterNeed(ResourceNeed need)
    {
        if (!ModelState.IsValid)
        {
            return View("Dashboard", need);
        }

        // Lagre i minnet
        NeedStore.AddOrUpdate(need);
        // Show the resources near the new need
        return RedirectToAction("Matches", new { needId = need.Id });
    }

    // Shows the resources within radiusKm of a need, nearest first.
    public IActionResult Matches(int needId, double radiusKm = 50)
    {
        var need = NeedStore.GetById(needId);
        if (need == null)
        {
            return RedirectToAction("DashboardIndex");
        }

        if (radiusKm <= 0)
        {
            radiusKm = 50;
        }

        var model = new MatchViewModel
        {
            Need = need,
            RadiusKm = radiusKm,
            Matches = RadiusMatcher.FindResources(need, radiusKm)
        };
        return View(model);
    }
}


public class HomeController : Controller
{
    public IActionResult Index(int? id)
    {
        if (id.HasValue)
        {
            var existingResource = ResourceStore.GetById(id.Value);
            if (existingResource != null)
            {
                return View(existingResource);
            }
        }
        
        // Sends an empty model to the form the first time the page is opened.
        return View(new ResourceEntry());     
    }
    
    [HttpPost]
    public async Task<IActionResult> SelectRole(string userType)
    {
        if (userType is not ("ResourceProvider" or "PublicActor"))
        {
            userType = "ResourceProvider";
        }

        var displayName = userType == "PublicActor" ? "Public Actor" : "Resource Provider";
        var claims = User.Claims
            .Where(claim => claim.Type is not (ClaimTypes.Name or ClaimTypes.Role or "UserType"))
            .ToList();
        claims.Add(new Claim(ClaimTypes.Name, displayName));
        claims.Add(new Claim(ClaimTypes.Role, userType));
        claims.Add(new Claim("UserType", userType));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true
            });

        if (userType == "PublicActor")
        {
            // Sender brukeren til Public Actor Dashboard
            return RedirectToAction("DashboardIndex", "PublicActor");
        }

        // Standard fallback: Sender til sivil/ressursleverandør sine sider
        return RedirectToAction("Index", "Home");
    }

    

    // Runs when the form is submitted, saves the resource, and redirects to the Resources page.
    [HttpPost]
    public IActionResult RegisterResource(ResourceEntry resource)
    {
        if (resource.ExpirationDate is null && !string.IsNullOrWhiteSpace(resource.ResourceType))
        {
            resource.ExpirationDate = GetExpiryDate(resource.ResourceType);
        }


        if (!ModelState.IsValid)
        {
            // sends the user to form with attributes and error messages filled out
            return View("Index", resource);
        }

        //save and update only when evrything is valid
        if (resource.Id > 0)
        {
            ResourceStore.Update(resource);
        }
        else
        {
            ResourceStore.Add(resource);
        }
        return RedirectToAction("Index", "Resources");
    }
    

    // Calculates the expiry date based on the resource category.
    private static DateTime? GetExpiryDate(string resourceType)
    {
        return resourceType switch
        {
            "Shelter" or "Transport" or "Other" => DateTime.Today.AddYears(1),
            "Food" => DateTime.Today.AddDays(14),
            "Medical" => DateTime.Today.AddMonths(1),
            "Materials" => DateTime.Today.AddMonths(6),
            _ => null
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public IActionResult DeleteResource(int id)
    {
        ResourceStore.Remove(id);
        return RedirectToAction("index", "Resources");
    }
}
