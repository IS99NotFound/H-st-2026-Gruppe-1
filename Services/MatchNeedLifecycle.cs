using H_st_2026_Gruppe_1.Data;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Services;

// Rules for how needs and resources change status when they are matched, released or closed.
public class MatchNeedLifecycle
{
    private readonly AppDbContext _db;
   
    public MatchNeedLifecycle(AppDbContext db)
    {
        _db = db;
    }

    //Trying to lock a resource top a need. Return false if someone else already assign it.
    public bool TryReserveResource(int resourceId)
    {
        var rows = _db.Resources
            .Where(r => r.Id == resourceId && r.Status == "available")
            .ExecuteUpdate(s => s.SetProperty(r => r.Status, "unavailable"));

        return rows == 1;
    }

    //Makes it so the resource becomes availible again, unless another "Need" still has it assigned.
    public void ReleaseResource(int resourceId)
    {
        var stillAssigned = _db.MatchRecords
            .Any(m => m.ResourceId == resourceId && m.Status == "accepted");

        if (stillAssigned)
        {
            return;
        }

        _db.Resources
            .Where(r => r.Id == resourceId && r.Status == "unavailable")
            .ExecuteUpdate(s => s.SetProperty(r => r.Status, "available"));
            
    } 

    //Sets a need-status based on the matches: assigned > under_review > new.
    public void RefreshNeedStatus(int needId)
    {
        var need = _db.Needs.Find(needId);
        if (need == null || need.Status is not ("new" or "under_review" or "assigned"))
        {
            return;
        }

        var statuses = _db.MatchRecords
            .Where(m => m.NeedId == needId)
            .Select(m => m.Status)
            .ToList();

        need.Status = statuses.Contains("accepted") ? "assigned"
            : statuses.Contains("proposed") ? "under_review"
            : "new";
        _db.SaveChanges();
    }


    //Will mark a need as resolved, and close and free a need
    public void CloseNeed(int needId, string needStatus = "resolved")
    {
        var need= _db.Needs.Find(needId);
        if (need == null)
        {
            return;
        }

        need.Status = needStatus;
        _db.SaveChanges();

        CompleteAcceptedMatches(_db.MatchRecords.Where(m => m.NeedId == needId && m.Status == "accepted"));
    }

    //Removes the matches from Needs, (fremmednøkkel)
    public void RemoveMatchesFromNeed(int needId)
    {
        var records = _db.MatchRecords.Where(m => m.NeedId == needId).ToList();
        var assignedResourceIds = records
            .Where(m => m.Status == "accepted")
            .Select(m => m.ResourceId)
            .ToList();

        _db.MatchRecords.RemoveRange(records);
        _db.SaveChanges();

        foreach (var resourceId in assignedResourceIds)
        {
            ReleaseResource(resourceId);
        }
    }

    //Will close assigned needs marked as resolved/cncelled or passed deadline
    public int ReleaseFinishedAssignments()
    {
        var now = DateTime.Now;

        // Needs where the deadline has passed are marked as resolved.
        var expiredNeeds = _db.Needs
            .Where(n => n.Deadline != null && n.Deadline < now
                    && (n.Status == "new" || n.Status == "under_review" || n.Status == "assigned"))
            .ToList();
        foreach (var need in expiredNeeds)
        {
            need.Status = "resolved";
        }
        _db.SaveChanges();

        // Find accepted matches that belong to a resolved or cancelled need.
        var finished = _db.MatchRecords
            .Where(m => m.Status == "accepted")
            .Join(_db.Needs, m => m.NeedId, n => n.Id, (m, n) => new { Match = m, Need = n })
            .Where(x => x.Need.Status == "resolved" || x.Need.Status == "cancelled")
            .Select(x => x.Match);

        return CompleteAcceptedMatches(finished);
    }

    //Accepted -> completed (memory remains), then the resource is "released"
    private int CompleteAcceptedMatches(IQueryable<Models.MatchRecord> query)
    {
        var records = query.ToList();
        foreach (var record in records)
        {
            record.Status = "completed";
        }
        _db.SaveChanges();

        foreach (var resourceId in records.Select(r => r.ResourceId).Distinct())
        {
            ReleaseResource(resourceId);
        }

        return records.Count;
    }

}

//Runs a "clean-up" every minute, so that a resource can be "released".
public class MatchLifecycleWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MatchLifecycleWorker> _logger;

    public MatchLifecycleWorker(IServiceScopeFactory scopeFactory, ILogger<MatchLifecycleWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Runs in the background as long as the app runs. It repeats the clean-up once a minute.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                scope.ServiceProvider.GetRequiredService<MatchNeedLifecycle>().ReleaseFinishedAssignments();
            }
            // An error must not stop the worker. We write it to the log and try again next minute.
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not release finished assigned resources");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}