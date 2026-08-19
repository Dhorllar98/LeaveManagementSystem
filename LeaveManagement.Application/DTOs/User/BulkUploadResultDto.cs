namespace LeaveManagement.Application.DTOs.User;

public class BulkUploadResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalProcessed { get; set; }
    public int SuccessfullyCreated { get; set; }
    public List<string> Errors { get; set; } = new();
}