namespace H_st_2026_Gruppe_1.Models;

public class ReportsIndexViewModel
{
    public int TotalIncidents { get; set; }
    public int ActiveIncidents { get; set; }
    public int ResolvedIncidents { get; set; }
    
    public int TotalNeeds { get; set; }
    public int UnansweredNeedsCount { get; set; }
    
    public int TotalResources { get; set; }
    public int AvailableResources { get; set; }
    public int ResourcesInUse { get; set; }

    public List<ReportResourceRowViewModel> Resources { get; set; } = new();
    public List<ReportIncidentRowViewModel> Incidents { get; set; } = new();
    public List<ReportNeedRowViewModel> Needs { get; set; } = new();
    public List<UnansweredNeedRowViewModel> UnansweredNeeds { get; set; } = new();
    public List<CategoryOptionViewModel> Categories { get; set; } = new();
    
    required 
    public DateTime? FromDate  { get; set; }
    public DateTime? ToDate { get; set; }
    public int? Category { get; set; }
    public string? Location { get; set; }
    
    public class ReportResourceRowViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public class ReportIncidentRowViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReportNeedRowViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public DateTime? Deadline { get; set; }
    }

    public class CategoryOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UnansweredNeedRowViewModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public string? Priority { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? Deadline { get; set; }
    }
    
    
}