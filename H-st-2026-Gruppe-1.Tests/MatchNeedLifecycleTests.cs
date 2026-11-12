using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using H_st_2026_Gruppe_1.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for MatchNeedLifecycle: reserving and releasing resources, and updating need status.
// This is the logic behind the "Assign" buttons on the matching page.
public class MatchNeedLifecycleTests : IDisposable
{
    private readonly TestDb _db = new TestDb();

    public MatchNeedLifecycleTests()
    {
        // Every resource and need needs a category, so one is always added
        _db.Seed(c => c.Categories.Add(TestData.NewCategory()));
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // ---------- Helpers ----------

    private MatchNeedLifecycle CreateService()
    {
        return new MatchNeedLifecycle(_db.CreateContext());
    }

    private void AddResource(int id, string status = "available")
    {
        _db.Seed(c => c.Resources.Add(TestData.NewResource(id, status: status)));
    }

    private void AddNeed(int id, string status = "new", DateTime? deadline = null)
    {
        _db.Seed(c => c.Needs.Add(TestData.NewNeed(id, status: status, deadline: deadline)));
    }

    private void AddMatch(int needId, int resourceId, string status)
    {
        _db.Seed(c => c.MatchRecords.Add(TestData.NewMatch(needId, resourceId, status)));
    }

    // The helpers below read with a NEW context, so we see what is really saved in the database
    private string ResourceStatus(int id)
    {
        using var context = _db.CreateContext();
        return context.Resources.AsNoTracking().Single(r => r.Id == id).Status;
    }

    private string NeedStatus(int id)
    {
        using var context = _db.CreateContext();
        return context.Needs.AsNoTracking().Single(n => n.Id == id).Status;
    }

    private string MatchStatus(int needId, int resourceId)
    {
        using var context = _db.CreateContext();
        return context.MatchRecords.AsNoTracking()
            .Single(m => m.NeedId == needId && m.ResourceId == resourceId).Status;
    }

    private int MatchCount(int needId)
    {
        using var context = _db.CreateContext();
        return context.MatchRecords.Count(m => m.NeedId == needId);
    }

    // ---------- TryReserveResource ----------

    [Fact]
    public void TryReserveResource_AvailableResource_ReturnsTrueAndLocksIt()
    {
        AddResource(1);

        var reserved = CreateService().TryReserveResource(1);

        Assert.True(reserved);
        Assert.Equal("unavailable", ResourceStatus(1));
    }

    [Fact]
    public void TryReserveResource_AlreadyUnavailable_ReturnsFalse()
    {
        AddResource(1, "unavailable");

        var reserved = CreateService().TryReserveResource(1);

        Assert.False(reserved);
        Assert.Equal("unavailable", ResourceStatus(1));
    }

    // ---------- ReleaseResource ----------

    [Fact]
    public void ReleaseResource_UnavailableAndNotAssigned_BecomesAvailable()
    {
        AddResource(1, "unavailable");

        CreateService().ReleaseResource(1);

        Assert.Equal("available", ResourceStatus(1));
    }

    [Fact]
    public void ReleaseResource_StillAcceptedByANeed_StaysUnavailable()
    {
        AddResource(1, "unavailable");
        AddNeed(1);
        AddMatch(1, 1, "accepted");

        CreateService().ReleaseResource(1);

        Assert.Equal("unavailable", ResourceStatus(1));
    }

    // ---------- RefreshNeedStatus ----------

    [Fact]
    public void RefreshNeedStatus_NoMatches_IsNew()
    {
        AddNeed(1, "under_review");

        CreateService().RefreshNeedStatus(1);

        Assert.Equal("new", NeedStatus(1));
    }

    [Fact]
    public void RefreshNeedStatus_ProposedMatch_IsUnderReview()
    {
        AddResource(1);
        AddNeed(1);
        AddMatch(1, 1, "proposed");

        CreateService().RefreshNeedStatus(1);

        Assert.Equal("under_review", NeedStatus(1));
    }

    [Fact]
    public void RefreshNeedStatus_AcceptedMatch_IsAssigned()
    {
        AddResource(1);
        AddNeed(1);
        AddMatch(1, 1, "accepted");

        CreateService().RefreshNeedStatus(1);

        Assert.Equal("assigned", NeedStatus(1));
    }

    [Fact]
    public void RefreshNeedStatus_AcceptedAndProposed_AssignedWins()
    {
        AddResource(1);
        AddResource(2);
        AddNeed(1);
        AddMatch(1, 1, "accepted");
        AddMatch(1, 2, "proposed");

        CreateService().RefreshNeedStatus(1);

        Assert.Equal("assigned", NeedStatus(1));
    }

    // ---------- CloseNeed ----------

    [Fact]
    public void CloseNeed_SetsStatusToResolved()
    {
        AddNeed(1, "assigned");

        CreateService().CloseNeed(1);

        Assert.Equal("resolved", NeedStatus(1));
    }

    [Fact]
    public void CloseNeed_CompletesAcceptedMatchesAndFreesTheResource()
    {
        AddResource(1, "unavailable");
        AddNeed(1, "assigned");
        AddMatch(1, 1, "accepted");

        CreateService().CloseNeed(1);

        Assert.Equal("completed", MatchStatus(1, 1));
        Assert.Equal("available", ResourceStatus(1));
    }

    // ---------- RemoveMatchesFromNeed ----------

    [Fact]
    public void RemoveMatchesFromNeed_FreesResourcesThatWereAssigned()
    {
        AddResource(1, "unavailable");
        AddNeed(1, "assigned");
        AddMatch(1, 1, "accepted");

        CreateService().RemoveMatchesFromNeed(1);

        Assert.Equal("available", ResourceStatus(1));
    }

    // ---------- ReleaseFinishedAssignments ----------

    [Theory]
    [InlineData("new")]
    public void ReleaseFinishedAssignments_PassedDeadline_ResolvesTheNeed(string status)
    {
        AddNeed(1, status, DateTime.Now.AddDays(-1));

        CreateService().ReleaseFinishedAssignments();

        Assert.Equal("resolved", NeedStatus(1));
    }

    [Fact]
    public void ReleaseFinishedAssignments_FutureDeadline_LeavesTheNeedAlone()
    {
        AddNeed(1, "assigned", DateTime.Now.AddDays(3));

        CreateService().ReleaseFinishedAssignments();

        Assert.Equal("assigned", NeedStatus(1));
    }

    // ---------- MatchLifecycleWorker (the background clean-up) ----------

    [Fact]
    public async Task Worker_RunsTheCleanUpWhenItStarts()
    {
        AddResource(1, "unavailable");
        AddNeed(1, "resolved");
        AddMatch(1, 1, "accepted");

        var services = new ServiceCollection();
        services.AddScoped(_ => _db.CreateContext());
        services.AddScoped<MatchNeedLifecycle>();
        using var provider = services.BuildServiceProvider();
        using var worker = new MatchLifecycleWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MatchLifecycleWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);

        // Wait until the clean-up has run (max 5 seconds), before the worker is stopped
        for (var i = 0; i < 50 && MatchStatus(1, 1) != "completed"; i++)
        {
            await Task.Delay(100);
        }

        await worker.StopAsync(CancellationToken.None);

        Assert.Equal("completed", MatchStatus(1, 1));
        Assert.Equal("available", ResourceStatus(1));
    }

    [Fact]
    public async Task Worker_SurvivesWhenTheCleanUpFails()
    {
        // No services are registered, so the clean-up throws. The worker must catch it and not crash.
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        using var worker = new MatchLifecycleWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MatchLifecycleWorker>.Instance);

        var exception = await Record.ExceptionAsync(async () =>
        {
            await worker.StartAsync(CancellationToken.None);
            await worker.StopAsync(CancellationToken.None);
        });

        Assert.Null(exception);
    }
}
