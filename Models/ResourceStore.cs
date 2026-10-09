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
    
    public static void Remove( int Id)
    {
        Resources.RemoveAll(x => x.Id == Id);
        
    }

    public static ResourceEntry? GetById(int Id)
    {
        return Resources.FirstOrDefault(x => x.Id == Id);
    }

    public static void Update(ResourceEntry UpdatedResource)
    {
        //finne feltene 
        var existingResource = Resources.FirstOrDefault(x => x.Id == UpdatedResource.Id);
        
        //hvis feltene eksisterer overskrive dem
        if (existingResource != null)
        {
            existingResource.Name = UpdatedResource.Name;
            existingResource.Description = UpdatedResource.Description;
            existingResource.ResourceType = UpdatedResource.ResourceType;
            existingResource.Latitude = UpdatedResource.Latitude;
            existingResource.Longitude = UpdatedResource.Longitude;
            existingResource.RegisteredAt = DateTime.Now;
            existingResource.Phone = UpdatedResource.Phone;
            existingResource.ExpirationDate =  UpdatedResource.ExpirationDate;
        }
        
    }
}