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
    [Required(ErrorMessage = "Dette feltet er påkrevd.")]
    [StringLength(150, ErrorMessage = "Tittelen kan ikke være lengre enn 150 tegn.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Dette feltet er påkrevd.")]
    public string IncidentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Dette feltet er påkrevd.")]
    [StringLength(4000, ErrorMessage = "Beskrivelsen kan ikke være lengre enn 4000 tegn.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Dette feltet er påkrevd.")]
    [Range(typeof(decimal), "-90", "90", ErrorMessage = "Breddegrad må være mellom -90 og 90.")]
    public decimal? Latitude { get; set; }

    [Required(ErrorMessage = "Dette feltet er påkrevd.")]
    [Range(typeof(decimal), "-180", "180", ErrorMessage = "Lengdegrad må være mellom -180 og 180.")]
    public decimal? Longitude { get; set; }
}
