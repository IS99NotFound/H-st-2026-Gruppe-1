namespace H_st_2026_Gruppe_1.Models;

public sealed record NeedReportViewModel(
    string Title,
    string? CategoryInfo,
    string Description,
    string LocationName,
    string Status,
    Enum Priority,
    string PhoneNumber,
    string Email,
    DateTime RegisteredAt
    );