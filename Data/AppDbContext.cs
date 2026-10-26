using H_st_2026_Gruppe_1.Models;
using Microsoft.EntityFrameworkCore;

namespace H_st_2026_Gruppe_1.Data;

// Connects the C# classes to the database tables
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    

    public DbSet<ResourceEntry> Resources => Set<ResourceEntry>();
    public DbSet<ResourceNeed> Needs => Set<ResourceNeed>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Incident> Incidents => Set<Incident>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Some columns have other names than the properties
        modelBuilder.Entity<ResourceEntry>(entity =>
        {
            entity.ToTable("Resource");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("ResourceID");
            entity.Property(r => r.ProviderId).HasColumnName("ProviderID");
            entity.Property(r => r.CategoryId).HasColumnName("CategoryID");
            entity.Property(r => r.RegionId).HasColumnName("RegionID");
            entity.Property(r => r.Phone).HasColumnName("ContactInfo");
            entity.Property(r => r.RegisteredAt).HasColumnName("CreatedAt");
            entity.HasOne(r => r.Category).WithMany().HasForeignKey(r => r.CategoryId);
        });

        modelBuilder.Entity<ResourceNeed>(entity =>
        {
            entity.ToTable("ResourceNeed");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Id).HasColumnName("NeedID");
            entity.Property(n => n.CrisisEventId).HasColumnName("IncidentID");
            entity.Property(n => n.PublicActorId).HasColumnName("PublicActorID");
            entity.Property(n => n.Category).HasColumnName("CategoryID");
            entity.Property(n => n.RegisteredAt).HasColumnName("CreatedAt");
            // Saved as text, the column is an ENUM
            entity.Property(n => n.Priority).HasConversion<string>();
            entity.HasOne(n => n.CategoryInfo).WithMany().HasForeignKey(n => n.Category);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(c => c.CategoryId);
            entity.Property(c => c.CategoryId).HasColumnName("CategoryID");
        });

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.ToTable("Incident");
            entity.HasKey(i => i.IncidentId);
            entity.Property(i => i.IncidentId).HasColumnName("IncidentID");
            entity.Property(i => i.PublicActorId).HasColumnName("PublicActorID");
            entity.Property(i => i.Latitude).HasPrecision(9, 6);
            entity.Property(i => i.Longitude).HasPrecision(9, 6);
        });
            
    }
    
}
