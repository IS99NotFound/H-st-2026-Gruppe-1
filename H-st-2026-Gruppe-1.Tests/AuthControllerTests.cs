using System.Security.Claims;
using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for AuthController: login with password, the two-factor code, and logout.
// UserManager and the authentication service are replaced with fakes (Moq), so no database is needed.
public class AuthControllerTests
{
    private const string GoodPassword = "Passord123!";

    // ---------- Helpers ----------

    // A fake UserManager. UserManager needs many constructor arguments, but the tests do not use them.
    private static Mock<UserManager<User>> NewUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static User NewUser(int id = 5, IdentityUserRole role = IdentityUserRole.CivilianProvider)
    {
        return new User { Id = id, UserName = "ola", Name = "Ola Nordmann", Role = role };
    }

    // Makes the fake UserManager find the user and accept the correct password
    private static void SetUpValidLogin(Mock<UserManager<User>> userManager, User user)
    {
        userManager.Setup(m => m.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(m => m.CheckPasswordAsync(user, GoodPassword)).ReturnsAsync(true);
        userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
    }

    private static AuthController NewController(Mock<UserManager<User>> userManager)
    {
        return new AuthController(userManager.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    // Does step 1 of the login (username + password) and returns the two-factor page model
    private static async Task<TwoFactorViewModel> StartLogin(AuthController controller, Mock<UserManager<User>> userManager, User user)
    {
        SetUpValidLogin(userManager, user);

        var result = await controller.Login(new LoginViewModel { UserName = user.UserName!, Password = GoodPassword });

        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsType<TwoFactorViewModel>(view.Model);
    }

    private static string WrongCodeFor(string realCode)
    {
        return realCode == "111111" ? "222222" : "111111";
    }

    private static List<string> ErrorsIn(Controller controller)
    {
        return controller.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .ToList();
    }

    // A fake HttpContext with a fake authentication service, so SignInAsync and SignOutAsync work in a test
    private static (DefaultHttpContext Context, Mock<IAuthenticationService> Auth) NewHttpContextWithAuth()
    {
        var auth = new Mock<IAuthenticationService>();
        auth.Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        auth.Setup(a => a.SignOutAsync(
                It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // RedirectToAction needs a URL helper, so we give it a fake one
        var urlFactory = new Mock<IUrlHelperFactory>();
        urlFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(new Mock<IUrlHelper>().Object);

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(auth.Object);
        services.AddSingleton<IUrlHelperFactory>(urlFactory.Object);

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        return (context, auth);
    }

    // ---------- Login (GET) ----------

    [Fact]
    public void LoginGet_NotLoggedIn_ShowsTheLoginPage()
    {
        var controller = NewController(NewUserManager());
        ControllerTestHelper.Attach(controller, null, null);

        var result = controller.Login();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<LoginViewModel>(view.Model);
    }

    // ---------- Login (POST): username and password ----------

    [Fact]
    public async Task Login_InvalidForm_ShowsTheFormAgainWithoutCheckingTheUser()
    {
        var userManager = NewUserManager();
        var controller = NewController(userManager);
        controller.ModelState.AddModelError("UserName", "Brukernavn er påkrevd.");
        var model = new LoginViewModel();

        var result = await controller.Login(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        userManager.Verify(m => m.FindByNameAsync(It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task Login_UnknownUser_GivesAGenericError()
    {
        var userManager = NewUserManager();
        userManager.Setup(m => m.FindByNameAsync("ukjent")).ReturnsAsync((User?)null);
        var controller = NewController(userManager);

        var result = await controller.Login(new LoginViewModel { UserName = "ukjent", Password = "x" });

        Assert.IsType<ViewResult>(result);
        Assert.Equal(new[] { "Ugyldig brukernavn eller passord." }, ErrorsIn(controller));
    }

    [Fact]
    public async Task Login_WrongPassword_GivesAGenericErrorAndCountsTheFailure()
    {
        var user = NewUser();
        var userManager = NewUserManager();
        SetUpValidLogin(userManager, user);
        userManager.Setup(m => m.CheckPasswordAsync(user, "feil")).ReturnsAsync(false);
        var controller = NewController(userManager);

        var result = await controller.Login(new LoginViewModel { UserName = "ola", Password = "feil" });

        Assert.IsType<ViewResult>(result);
        Assert.Equal(new[] { "Ugyldig brukernavn eller passord." }, ErrorsIn(controller));
        userManager.Verify(m => m.AccessFailedAsync(user), Times.Once());
    }

    [Fact]
    public async Task Login_LockedOutUser_IsRejectedEvenWithTheRightPassword()
    {
        var user = NewUser();
        var userManager = NewUserManager();
        SetUpValidLogin(userManager, user);
        userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);
        var controller = NewController(userManager);

        var result = await controller.Login(new LoginViewModel { UserName = "ola", Password = GoodPassword });

        Assert.IsType<ViewResult>(result);
        Assert.Equal(new[] { "Ugyldig brukernavn eller passord." }, ErrorsIn(controller));
        userManager.Verify(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task Login_CorrectPassword_ShowsTheTwoFactorPage()
    {
        var user = NewUser();
        var userManager = NewUserManager();
        SetUpValidLogin(userManager, user);
        var controller = NewController(userManager);

        var result = await controller.Login(new LoginViewModel { UserName = "ola", Password = GoodPassword });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("TwoFactor", view.ViewName);
        Assert.IsType<TwoFactorViewModel>(view.Model);
        Assert.Empty(ErrorsIn(controller));
    }

    // ---------- Two-factor (GET) ----------

    // ---------- VerifyTwoFactor (POST): the six digit code ----------

    [Fact]
    public async Task VerifyTwoFactor_MissingPendingId_GoesBackToLogin()
    {
        var controller = NewController(NewUserManager());

        var result = await controller.VerifyTwoFactor(new TwoFactorViewModel { PendingId = "", Code = "123456" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
    }

    [Fact]
    public async Task VerifyTwoFactor_WrongCode_ShowsTheTwoFactorPageAgain()
    {
        var userManager = NewUserManager();
        var controller = NewController(userManager);
        var start = await StartLogin(controller, userManager, NewUser());

        var result = await controller.VerifyTwoFactor(new TwoFactorViewModel
        {
            PendingId = start.PendingId,
            Code = WrongCodeFor(start.DemoCode!)
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("TwoFactor", view.ViewName);
        Assert.True(controller.ModelState.ContainsKey(nameof(TwoFactorViewModel.Code)));
    }

    [Fact]
    public async Task VerifyTwoFactor_FourthAttempt_IsBlockedEvenWithTheRightCode()
    {
        var userManager = NewUserManager();
        var controller = NewController(userManager);
        var start = await StartLogin(controller, userManager, NewUser());
        var wrong = WrongCodeFor(start.DemoCode!);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await controller.VerifyTwoFactor(new TwoFactorViewModel { PendingId = start.PendingId, Code = wrong });
        }

        var result = await controller.VerifyTwoFactor(new TwoFactorViewModel
        {
            PendingId = start.PendingId,
            Code = start.DemoCode
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
    }

    // ---------- VerifyTwoFactor (POST): successful login ----------

    [Fact]
    public async Task VerifyTwoFactor_CorrectCodeForProvider_LogsInAndGoesToHome()
    {
        var userManager = NewUserManager();
        var user = NewUser(5, IdentityUserRole.CivilianProvider);
        userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(user);
        var controller = NewController(userManager);
        var start = await StartLogin(controller, userManager, user);
        var (httpContext, _) = NewHttpContextWithAuth();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.VerifyTwoFactor(new TwoFactorViewModel
        {
            PendingId = start.PendingId,
            Code = start.DemoCode
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task VerifyTwoFactor_CorrectCodeForPublicActor_GoesToTheDashboard()
    {
        var userManager = NewUserManager();
        var user = NewUser(1, IdentityUserRole.PublicActor);
        userManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
        var controller = NewController(userManager);
        var start = await StartLogin(controller, userManager, user);
        var (httpContext, _) = NewHttpContextWithAuth();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.VerifyTwoFactor(new TwoFactorViewModel
        {
            PendingId = start.PendingId,
            Code = start.DemoCode
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("DashboardIndex", redirect.ActionName);
        Assert.Equal("PublicActor", redirect.ControllerName);
    }

    [Fact]
    public async Task VerifyTwoFactor_CorrectCode_CanOnlyBeUsedOnce()
    {
        var userManager = NewUserManager();
        var user = NewUser(5);
        userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(user);
        var controller = NewController(userManager);
        var start = await StartLogin(controller, userManager, user);
        var (httpContext, _) = NewHttpContextWithAuth();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var model = new TwoFactorViewModel { PendingId = start.PendingId, Code = start.DemoCode };

        await controller.VerifyTwoFactor(model);
        var second = await controller.VerifyTwoFactor(model);

        var redirect = Assert.IsType<RedirectToActionResult>(second);
        Assert.Equal("Login", redirect.ActionName);
    }

    // ---------- Logout ----------

    [Fact]
    public async Task Logout_SignsOutAndGoesToLogin()
    {
        var controller = NewController(NewUserManager());
        var (httpContext, auth) = NewHttpContextWithAuth();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Logout();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        auth.Verify(a => a.SignOutAsync(
            It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Once());
    }
}
