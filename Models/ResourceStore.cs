namespace H_st_2026_Gruppe_1.Models;

// Enkel liste i minnet siden vi ikke har database, nullstilles ved restart av serveren
public static class ResourceStore
{
    public static List<ResourceEntry> Resources = new List<ResourceEntry>();
    private static int nextId = 1;

    public static void Add(ResourceEntry resource)
    {
        resource.Id = nextId;
        nextId++;
        resource.RegisteredAt = DateTime.Now;
        Resources.Add(resource);
    }
}