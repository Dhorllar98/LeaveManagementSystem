namespace LeaveManagement.Application.DTOs.Profile;

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Designation { get; set; }
}