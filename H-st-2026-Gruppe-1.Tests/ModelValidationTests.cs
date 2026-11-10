using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for the validation rules on the models and view models ([Required], [StringLength], [Range] ...).
// ValidationHelper runs the same checks that ASP.NET runs when a form is submitted.

// ---------- ResourceEntry (a resource registered by a provider) ----------
public class ResourceEntryValidationTests
{
    private static ResourceEntry ValidResource()
    {
        return new ResourceEntry
        {
            Name = "Ola Nordmann",
            Email = "ola@test.no",
            PhoneNumber = "+47 90010101",
            CategoryId = 1,
            Description = "Traktor med frontlaster",
            LocationName = "Kristiansand",
            Latitude = 58.1467,
            Longitude = 7.9956
        };
    }

    [Fact]
    public void ValidResource_HasNoErrors()
    {
        Assert.True(ValidationHelper.IsValid(ValidResource()));
    }

    [Fact]
    public void Name_Empty_IsInvalid()
    {
        var resource = ValidResource();
        resource.Name = "";

        Assert.True(ValidationHelper.HasErrorFor(resource, nameof(ResourceEntry.Name)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    public void Email_Invalid_IsRejected(string email)
    {
        var resource = ValidResource();
        resource.Email = email;

        Assert.True(ValidationHelper.HasErrorFor(resource, nameof(ResourceEntry.Email)));
    }

    [Fact]
    public void ExpirationDate_BeforeAvailableFrom_IsInvalid()
    {
        var resource = ValidResource();
        resource.AvailableFrom = new DateTime(2026, 11, 10);
        resource.ExpirationDate = new DateTime(2026, 11, 9);

        Assert.True(ValidationHelper.HasErrorFor(resource, nameof(ResourceEntry.ExpirationDate)));
    }
}

// ---------- ResourceNeed (a need registered by a public actor) ----------
public class ResourceNeedValidationTests
{
    private static ResourceNeed ValidNeed()
    {
        return new ResourceNeed
        {
            Title = "Trenger aggregat",
            Email = "politiet@test.no",
            PhoneNumber = "+47 90010101",
            CrisisEventId = 1,
            Category = 1,
            Description = "Strømbrudd i bygget",
            Quantity = 1,
            LocationName = "Kristiansand",
            Latitude = 58.1467,
            Longitude = 7.9956
        };
    }

    [Fact]
    public void ValidNeed_HasNoErrors()
    {
        Assert.True(ValidationHelper.IsValid(ValidNeed()));
    }

    [Fact]
    public void Title_Empty_IsInvalid()
    {
        var need = ValidNeed();
        need.Title = "";

        Assert.True(ValidationHelper.HasErrorFor(need, nameof(ResourceNeed.Title)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_OutsideAllowedRange_IsInvalid(int quantity)
    {
        var need = ValidNeed();
        need.Quantity = quantity;

        Assert.True(ValidationHelper.HasErrorFor(need, nameof(ResourceNeed.Quantity)));
    }

    [Fact]
    public void Deadline_BeforeRequiredFrom_IsInvalid()
    {
        var need = ValidNeed();
        need.RequiredFrom = new DateTime(2026, 11, 10);
        need.Deadline = new DateTime(2026, 11, 9);

        Assert.True(ValidationHelper.HasErrorFor(need, nameof(ResourceNeed.Deadline)));
    }
}

// ---------- ResourceViewModel (the resource form) ----------
public class ResourceViewModelValidationTests
{
    private static ResourceViewModel ValidModel()
    {
        return new ResourceViewModel
        {
            Name = "Kari Hansen",
            Phone = "kari@test.no",
            Description = "Gravemaskin",
            ResourceType = "Maskiner",
            Latitude = 58.1,
            Longitude = 7.9
        };
    }

    [Theory]
    [InlineData("kari@test.no")]
    [InlineData("kari.hansen@firma.co.uk")]
    public void Phone_ValidEmailOrPhoneNumber_IsAccepted(string value)
    {
        var model = ValidModel();
        model.Phone = value;

        Assert.False(ValidationHelper.HasErrorFor(model, nameof(ResourceViewModel.Phone)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void Phone_InvalidValue_IsRejected(string value)
    {
        var model = ValidModel();
        model.Phone = value;

        Assert.True(ValidationHelper.HasErrorFor(model, nameof(ResourceViewModel.Phone)));
    }
}

// ---------- RegisterViewModel (the registration form) ----------
public class RegisterViewModelValidationTests
{
    private static RegisterViewModel ValidModel()
    {
        return new RegisterViewModel
        {
            Name = "Ola Nordmann",
            UserName = "ola",
            Email = "ola@test.no",
            Password = "Passord123!",
            ConfirmPassword = "Passord123!"
        };
    }

    [Fact]
    public void ValidModel_HasNoErrors()
    {
        Assert.True(ValidationHelper.IsValid(ValidModel()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void Password_TooShort_IsInvalid(string password)
    {
        var model = ValidModel();
        model.Password = password;
        model.ConfirmPassword = password;

        Assert.True(ValidationHelper.HasErrorFor(model, nameof(RegisterViewModel.Password)));
    }

    [Fact]
    public void ConfirmPassword_NotTheSame_IsInvalid()
    {
        var model = ValidModel();
        model.ConfirmPassword = "AnnetPassord1!";

        Assert.True(ValidationHelper.HasErrorFor(model, nameof(RegisterViewModel.ConfirmPassword)));
    }
}

// ---------- IncidentCreateViewModel (the incident form) ----------
public class IncidentCreateViewModelValidationTests
{
    private static IncidentCreateViewModel ValidModel()
    {
        return new IncidentCreateViewModel
        {
            Title = "Flom i sentrum",
            IncidentType = "Flood",
            Description = "Vann i gatene",
            Latitude = 58.1467m,
            Longitude = 7.9956m
        };
    }

    [Fact]
    public void ValidModel_HasNoErrors()
    {
        Assert.True(ValidationHelper.IsValid(ValidModel()));
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Latitude_OutsideRange_IsInvalid(double latitude)
    {
        var model = ValidModel();
        model.Latitude = (decimal)latitude;

        Assert.True(ValidationHelper.HasErrorFor(model, nameof(IncidentCreateViewModel.Latitude)));
    }
}

// ---------- LoginViewModel and TwoFactorViewModel (login pages) ----------
public class LoginViewModelValidationTests
{

    [Fact]
    public void Login_WithoutUserName_IsInvalid()
    {
        var model = new LoginViewModel { UserName = "", Password = "hemmelig" };

        Assert.True(ValidationHelper.HasErrorFor(model, nameof(LoginViewModel.UserName)));
    }
}

public class TwoFactorViewModelValidationTests
{
    [Theory]
    [InlineData("123456")]
    [InlineData("000000")]
    public void Code_WithSixDigits_IsValid(string code)
    {
        var model = new TwoFactorViewModel { PendingId = "abc", Code = code };

        Assert.False(ValidationHelper.HasErrorFor(model, nameof(TwoFactorViewModel.Code)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    public void Code_NotSixDigits_IsInvalid(string code)
    {
        var model = new TwoFactorViewModel { PendingId = "abc", Code = code };

        Assert.True(ValidationHelper.HasErrorFor(model, nameof(TwoFactorViewModel.Code)));
    }
}
