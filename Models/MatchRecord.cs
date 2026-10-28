namespace H_st_2026_Gruppe_1.Models;

// One row in the ResourceMatch table: the decision a public actor made about a resource for a need.
// Status is one of: proposed (under review), accepted (assigned), declined (rejected), completed.
public class MatchRecord
{
    public int NeedId { get; set; }
    public int ResourceId { get; set; }
    public int PublicActorId { get; set; }
    public int Quantity { get; set; } = 1;
    public string Status { get; set; } = "proposed";
    public string? Comment { get; set; }
    public DateTime MatchDate { get; set; }
}
