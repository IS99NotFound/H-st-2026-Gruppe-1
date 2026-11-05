using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for AppDbContext: that the C# classes are saved in the right tables and columns.
// The tests use SQLite in memory (see TestDb), so they run without Docker.
public class AppDbContextTests : IDisposable
{
    private readonly TestDb _db = new TestDb();

    public void Dispose()
    {
        _db.Dispose();
    }

    // Counts the rows in a table with plain SQL. If the table or column name is wrong, this fails.
    private long CountRows(string table)
    {
        using var context = _db.CreateContext();
        return context.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM \"" + table + "\"")
            .AsEnumerable()
            .Single();
    }

    // ---------- Table names ----------

    [Theory]
    [InlineData("Resource")]
    [InlineData("ResourceNeed")]
    [InlineData("Category")]
    public void TableExists_AndStartsEmpty(string table)
    {
        Assert.Equal(0, CountRows(table));
    }

    // ---------- Saving and reading each entity ----------

    [Fact]
    public void Resource_CanBeSavedAndRead()
    {
        _db.Seed(c =>
        {
            c.Categories.Add(TestData.NewCategory());
            c.Resources.Add(TestData.NewResource(10, name: "Traktor"));
        });

        using var context = _db.CreateContext();
        var resource = context.Resources.Single();

        Assert.Equal(10, resource.Id);
        Assert.Equal("Traktor", resource.Name);
        Assert.Equal("available", resource.Status);
        Assert.Equal(58.1467, resource.Latitude);
        Assert.Equal(7.9956, resource.Longitude);
    }

    [Fact]
    public void Resource_WithoutPosition_IsRejectedByTheDatabase()
    {
        // The position columns are required, so a resource must always have a latitude and longitude
        Assert.Throws<DbUpdateException>(() => _db.Seed(c =>
        {
            c.Categories.Add(TestData.NewCategory());
            c.Resources.Add(TestData.NewResource(1, latitude: null, longitude: null));
        }));
    }

    // ---------- Column names and conversions ----------

    [Fact]
    public void NeedPriority_IsStoredAsText()
    {
        _db.Seed(c =>
        {
            c.Categories.Add(TestData.NewCategory());
            c.Needs.Add(TestData.NewNeed(1, priority: NeedPriorityEnum.Critical));
        });

        using var context = _db.CreateContext();
        var stored = context.Database
            .SqlQueryRaw<string>("SELECT Priority AS Value FROM ResourceNeed")
            .AsEnumerable()
            .Single();

        Assert.Equal("Critical", stored);
    }

    // ---------- Keys ----------

    [Fact]
    public void MatchRecord_SameNeedAndResourceTwice_IsRejected()
    {
        _db.Seed(c => c.MatchRecords.Add(TestData.NewMatch(1, 1)));

        using var context = _db.CreateContext();
        context.MatchRecords.Add(TestData.NewMatch(1, 1, "accepted"));

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
