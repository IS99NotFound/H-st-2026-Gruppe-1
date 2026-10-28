namespace H_st_2026_Gruppe_1.Models;

public class Incident
{
        public int IncidentId { get; set; }
        public int PublicActorId { get; set; }
        public string Title { get; set; } = "";
        public string IncidentType { get; set; } = "Other";
        public string? Description { get; set; }
        public string Status { get; set; } = "active";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
    
}