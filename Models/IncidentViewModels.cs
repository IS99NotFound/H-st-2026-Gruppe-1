using System.ComponentModel.DataAnnotations;

namespace H_st_2026_Gruppe_1.Models;

public class IncidentIndexViewModel
{
    public IReadOnlyList<Incident> Incidents { get; set; } = [];
    public IncidentCreateViewModel CreateModel { get; set; } = new();
    public string? Search { get; set; }
}

public class IncidentCreateViewModel
{
    [Required(ErrorMessage = "This field is required.")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "This field is required.")]
    public string IncidentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "This field is required.")]
    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "This field is required.")]
    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; }

    [Required(ErrorMessage = "This field is required.")]
    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; }
}
