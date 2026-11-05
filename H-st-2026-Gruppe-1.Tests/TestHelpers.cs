using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace H_st_2026_Gruppe_1.Tests;

// ------------------------------------------------------------
// Shared helper classes used by many test files.
// ------------------------------------------------------------

// A fresh, empty database for ONE test.
// It uses SQLite in memory instead of MariaDB, so no Docker is needed to run the tests.
// The database is deleted when the connection is closed (in Dispose).
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public DbContextOptions<AppDbContext> Options { get; }

    public TestDb()
    {
        // The database only lives as long as this connection is open
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Creates all tables from the model in AppDbContext
        using var context = new AppDbContext(Options);
        context.Database.EnsureCreated();
    }

    // Gives a new context that talks to the same in-memory database
    public AppDbContext CreateContext()
    {
        return new AppDbContext(Options);
    }

    // Runs some code with a context and saves afterwards. Used to fill the database before a test.
    public void Seed(Action<AppDbContext> action)
    {
        using var context = CreateContext();
        action(context);
        context.SaveChanges();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}

// Small methods that create test data, so each test only sets what it cares about.
public static class TestData
{
    public static Category NewCategory(int id = 1, string name = "Transport")
    {
        return new Category { CategoryId = id, CategoryName = name };
    }

    public static ResourceEntry NewResource(
        int id,
        int providerId = 2,
        int categoryId = 1,
        string name = "Aggregat",
        string status = "available",
        double? latitude = 58.1467,
        double? longitude = 7.9956)
    {
        return new ResourceEntry
        {
            Id = id,
            Name = name,
            Email = "test@test.no",
            PhoneNumber = "+47 90010101",
            CategoryId = categoryId,
            ProviderId = providerId,
            RegionId = 8,
            Description = "Testressurs",
            AvailableFrom = new DateTime(2026, 11, 1),
            RegisteredAt = new DateTime(2026, 11, 1),
            Latitude = latitude,
            Longitude = longitude,
            LocationName = "Kristiansand",
            Status = status
        };
    }

    public static ResourceNeed NewNeed(
        int id,
        int publicActorId = 1,
        int categoryId = 1,
        string title = "Trenger strøm",
        string status = "new",
        NeedPriorityEnum priority = NeedPriorityEnum.Planned,
        double latitude = 58.1467,
        double longitude = 7.9956,
        DateTime? deadline = null)
    {
        return new ResourceNeed
        {
            Id = id,
            Title = title,
            Email = "behov@test.no",
            PhoneNumber = "+47 90010101",
            CrisisEventId = 1,
            Category = categoryId,
            PublicActorId = publicActorId,
            Description = "Testbehov",
            Quantity = 1,
            Priority = priority,
            Deadline = deadline,
            LocationName = "Kristiansand",
            Latitude = latitude,
            Longitude = longitude,
            RegisteredAt = new DateTime(2026, 11, 1),
            Status = status
        };
    }

    public static Incident NewIncident(
        int id,
        int publicActorId = 1,
        string title = "Brann i lagerhall",
        string status = "active",
        string incidentType = "Fire",
        DateTime? createdAt = null)
    {
        return new Incident
        {
            IncidentId = id,
            PublicActorId = publicActorId,
            Title = title,
            NormalizedTitle = title.Trim().ToLowerInvariant(),
            IncidentType = incidentType,
            Description = "Testhendelse",
            Status = status,
            Latitude = 58.1467m,
            Longitude = 7.9956m,
            CreatedAt = createdAt ?? new DateTime(2026, 11, 1, 12, 0, 0)
        };
    }

    public static MatchRecord NewMatch(int needId, int resourceId, string status = "proposed", int publicActorId = 1)
    {
        return new MatchRecord
        {
            NeedId = needId,
            ResourceId = resourceId,
            PublicActorId = publicActorId,
            Status = status,
            MatchDate = new DateTime(2026, 11, 2)
        };
    }
}

// Sets up a controller so it behaves like a request from a logged-in user.
public static class ControllerTestHelper
{
    // Builds the user (claims) that the controller sees in "User".
    // userId and role are both null = a user that is not logged in.
    public static ClaimsPrincipal BuildUser(int? userId, string? role)
    {
        if (userId == null && role == null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    // Gives the controller a fake HttpContext with the user, and a working TempData.
    public static void Attach(Controller controller, int? userId, string? role)
    {
        var httpContext = new DefaultHttpContext { User = BuildUser(userId, role) };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider());
    }

    // Same, but for API controllers (they have no TempData)
    public static void AttachApi(ControllerBase controller, int? userId, string? role)
    {
        var httpContext = new DefaultHttpContext { User = BuildUser(userId, role) };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }
}

// TempData needs a "provider" that stores the values. This one stores nothing, which is fine in a test.
public sealed class InMemoryTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object?> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object?>();
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object?> values)
    {
    }
}

// A fake "environment" so we can choose between Development and Production in a test.
public sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public FakeWebHostEnvironment(string environmentName)
    {
        EnvironmentName = environmentName;
    }

    public string ApplicationName { get; set; } = "Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public string EnvironmentName { get; set; }
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

// The normal fake HttpContext never runs "OnStarting" callbacks.
// This feature remembers them so a test can run them by hand (used for the security headers).
public sealed class RecordingResponseFeature : IHttpResponseFeature
{
    private readonly List<(Func<object, Task> Callback, object State)> _callbacks = new();

    public int StatusCode { get; set; } = 200;
    public string? ReasonPhrase { get; set; }
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public Stream Body { get; set; } = Stream.Null;
    public bool HasStarted { get; private set; }

    public void OnStarting(Func<object, Task> callback, object state)
    {
        _callbacks.Add((callback, state));
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
    }

    // Runs all the remembered callbacks, like the server does when the response starts
    public async Task FireOnStartingAsync()
    {
        HasStarted = true;
        foreach (var entry in _callbacks)
        {
            await entry.Callback(entry.State);
        }
    }
}

// A fake HttpClient handler. The test decides what answer comes back, so no real internet is used.
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public List<HttpRequestMessage> Requests { get; } = new();

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(_responder(request));
    }
}

// Hands out HttpClients that use the stub handler above
public sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler, disposeHandler: false);
    }
}

// Runs the validation attributes ([Required], [StringLength] ...) on a model, like ASP.NET does.
public static class ValidationHelper
{
    public static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    public static bool IsValid(object model)
    {
        return Validate(model).Count == 0;
    }

    // True if the given property has at least one validation error
    public static bool HasErrorFor(object model, string propertyName)
    {
        return Validate(model).Any(result => result.MemberNames.Contains(propertyName));
    }
}
