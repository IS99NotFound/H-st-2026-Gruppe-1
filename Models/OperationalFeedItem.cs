namespace H_st_2026_Gruppe_1.Models;

public class OperationalFeedItem
{
    public string Type { get; set; } = "";
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string? Category { get; set; }
    public string Priority { get; set; } = "";
    public string ColorKey { get; set; } = "public-team";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
}
