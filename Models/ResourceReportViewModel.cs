namespace H_st_2026_Gruppe_1.Models;

public sealed record ResourceReportRow(
    string Name,
    string Category,
    string Description,
    string LocationName,
    string Email,
    string PhoneNumber,
    string Status,
    DateTime RegisteredAt);
