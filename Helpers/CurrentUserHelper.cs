using System.Security.Claims;
using H_st_2026_Gruppe_1.Data;

namespace H_st_2026_Gruppe_1.Helpers;

// Finner den innloggede brukeren og organisasjonen til brukeren.
public static class CurrentUserHelper
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        return int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }

    public static int? GetOrganizationId(this AppDbContext db, int? userId)
    {
        return db.PublicActors
            .Where(actor => actor.PublicActorId == userId)
            .Select(actor => (int?)actor.OrganizationId)
            .FirstOrDefault();
    }

    // Alle offentlige aktører i organisasjonen
    public static HashSet<int> GetOrganizationMemberIds(this AppDbContext db, int? organizationId)
    {
        return db.PublicActors
            .Where(actor => actor.OrganizationId == organizationId)
            .Select(actor => actor.PublicActorId)
            .ToHashSet();
    }
}
