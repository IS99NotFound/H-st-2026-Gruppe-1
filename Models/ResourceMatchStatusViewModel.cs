namespace H_st_2026_Gruppe_1.Models;

public class ResourceMatchStatusViewModel
{
    public int ResourceId { get; set; }
    public int NeedId { get; set; }
    public string NeedTitle { get; set; } = "";
    public string NeedLocation { get; set; } = "";
    public DateTime? NeedDeadline { get; set; }
    public NeedPriorityEnum NeedPriority { get; set; }
    public string NeedEmail { get; set; } = "";
    public string NeedPhone { get; set; } = "";
    public string Status { get; set; }
    public string? Comment { get; set; }
    
}