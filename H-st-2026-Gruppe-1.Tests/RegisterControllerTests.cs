using H_st_2026_Gruppe_1.Controllers;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for RegisterController: creating a new user account.
// Most tests run as "Production", because that is the setting where the rules are enforced.
public class RegisterControllerTests : IDisposable
{
    private readonly TestDb _db = new TestDb();

    public void Dispose()
    {
        _db.Dispose();
    }

    // ---------- Helpers ----------

    private static Mock<UserManager<User>> NewUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private RegisterController NewController(Mock<UserManager<User>> userManager, string environment = "Production")
    {
        var controller = new RegisterController(
            userManager.Object,
            _db.CreateContext(),
            new FakeWebHostEnvironment(environment));

        // The controller writes a message to TempData after a successful registration
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider());
        return controller;
    }

    private static RegisterViewModel ValidModel(IdentityUserRole role = IdentityUserRole.CivilianProvider)
    {
        return new RegisterViewModel
        {
            Name = "ola NORDMANN",
            UserName = "ola",
            Email = "ola@test.no",
            PhoneNumber = "+47 90010101",
            Role = role,
            Password = "Passord123!",
            ConfirmPassword = "Passord123!"
        };
    }

    // Makes the fake UserManager accept the new user and give it id 7
    private static void AcceptNewUsers(Mock<UserManager<User>> userManager, Action<User>? onCreated = null)
    {
        userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, _) =>
            {
                user.Id = 7;
                onCreated?.Invoke(user);
            })
            .ReturnsAsync(IdentityResult.Success);
    }

    private static List<string> ErrorsFor(Controller controller, string key)
    {
        if (!controller.ModelState.TryGetValue(key, out var entry))
        {
            return new List<string>();
        }

        return entry.Errors.Select(error => error.ErrorMessage).ToList();
    }

    private void AddOrganization(int id = 1, string name = "Politiet")
    {
        _db.Seed(c => c.Organizations.Add(new Organization { OrganizationId = id, OrganizationName = name }));
    }

    // ---------- Register (GET) ----------

    // ---------- Register (POST): role rules ----------

    [Fact]
    public async Task Post_Production_AdministratorCannotSelfRegister()
    {
        var userManager = NewUserManager();
        var controller = NewController(userManager);

        var result = await controller.Index(ValidModel(IdentityUserRole.Administrator));

        Assert.IsType<ViewResult>(result);
        Assert.NotEmpty(ErrorsFor(controller, nameof(RegisterViewModel.Role)));
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task Post_Production_PublicActorCannotSelfRegister()
    {
        AddOrganization();
        var userManager = NewUserManager();
        var controller = NewController(userManager);
        var model = ValidModel(IdentityUserRole.PublicActor);
        model.OrganizationId = 1;

        var result = await controller.Index(model);

        Assert.IsType<ViewResult>(result);
        Assert.Contains("Bare ressursleverandører kan registrere seg.", ErrorsFor(controller, nameof(RegisterViewModel.Role)));
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never());
    }

    [Fact]
    public async Task Post_PublicActorWithoutAValidOrganization_GetsAnOrganizationError()
    {
        var controller = NewController(NewUserManager());
        var model = ValidModel(IdentityUserRole.PublicActor);
        model.OrganizationId = 123;

        await controller.Index(model);

        Assert.NotEmpty(ErrorsFor(controller, nameof(RegisterViewModel.OrganizationId)));
    }

    // ---------- Register (POST): a valid provider ----------

    [Fact]
    public async Task Post_ValidProvider_RedirectsToLogin()
    {
        var userManager = NewUserManager();
        AcceptNewUsers(userManager);
        var controller = NewController(userManager);

        var result = await controller.Index(ValidModel());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Auth", redirect.ControllerName);
    }

    [Fact]
    public async Task Post_ValidProvider_SavesAProviderRowWithTheUsersId()
    {
        var userManager = NewUserManager();
        AcceptNewUsers(userManager);
        var controller = NewController(userManager);

        await controller.Index(ValidModel());

        using var context = _db.CreateContext();
        var provider = context.CivilianProviders.Single();
        Assert.Equal(7, provider.ProviderId);
        Assert.Equal("private_person", provider.ProviderType);
    }

    // ---------- Register (POST): problems ----------

    [Fact]
    public async Task Post_InvalidForm_ShowsTheFormAgainWithoutCreatingAUser()
    {
        var userManager = NewUserManager();
        var controller = NewController(userManager);
        controller.ModelState.AddModelError("Name", "Navn er påkrevd.");

        var result = await controller.Index(ValidModel());

        Assert.IsType<ViewResult>(result);
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never());
    }

    [Theory]
    [InlineData("PasswordRequiresDigit", "Passordet må inneholde minst ett tall.")]
    public async Task Post_IdentityError_IsShownInNorwegian(string code, string expectedMessage)
    {
        var userManager = NewUserManager();
        userManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = code, Description = "English text" }));
        var controller = NewController(userManager);

        var result = await controller.Index(ValidModel());

        Assert.IsType<ViewResult>(result);
        Assert.Contains(expectedMessage, ErrorsFor(controller, string.Empty));
    }
}
