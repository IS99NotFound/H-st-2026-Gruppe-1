namespace H_st_2026_Gruppe_1.Models;

public class ErrorViewModel
{
    // Id of the request that failed. It helps when searching for the error in the logs.
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
