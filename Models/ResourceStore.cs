namespace H_st_2026_Gruppe_1.Models;

// Enkel liste i minnet siden vi ikke har koblet opp mot database, nullstilles ved restart av serveren
public static class ResourceStore
{
    // Alle registrerte ressurser
    public static List<ResourceEntry> Resources = new List<ResourceEntry>();

    // Neste ledige Id, teller opp for hver ressurs
    private static int nextId = 1;

    // Legger til en ny ressurs og setter Id og registreringstidspunkt
    public static void Add(ResourceEntry resource)
    {
        resource.Id = nextId;
        nextId++;
        resource.RegisteredAt = DateTime.Now;
        Resources.Add(resource);
    }
}