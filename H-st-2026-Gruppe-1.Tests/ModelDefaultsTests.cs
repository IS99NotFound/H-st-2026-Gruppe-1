using H_st_2026_Gruppe_1.Data;
using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for the default values on the model classes and for small helper classes.
// The rest of the program depends on these defaults (for example that a new need has status "new").
public class ModelDefaultsTests
{
    // ---------- ResourceNeed ----------

    [Fact]
    public void NewNeed_StartsWithStatusNew()
    {
        Assert.Equal("new", new ResourceNeed().Status);
    }

    // ---------- ResourceEntry ----------

    // ---------- Incident ----------

    // ---------- MatchRecord ----------

    // ---------- Users and roles ----------

    [Theory]
    [InlineData(IdentityUserRole.PublicActor, "PublicActor")]
    [InlineData(IdentityUserRole.CivilianProvider, "CivilianProvider")]
    [InlineData(IdentityUserRole.Administrator, "Administrator")]
    public void IdentityUserRole_ToStringMatchesTheRoleNamesUsedInAuthorizeAttributes(IdentityUserRole role, string expected)
    {
        // The controllers use [Authorize(Roles = "PublicActor")] etc, so the text must be exactly the same
        Assert.Equal(expected, role.ToString());
    }

    // ---------- NeedPriorityEnum ----------

    // ---------- View models ----------

    // ---------- DemoDefaults ----------
}
