namespace H_st_2026_Gruppe_1.Models;

public sealed record IncidentReportRow(
    string Title,
    string IncidentType,
    string? Description,
    string RegionName,
    decimal Latitude,
    decimal Longitude,
    string Status,
    DateTime CreatedAt);
