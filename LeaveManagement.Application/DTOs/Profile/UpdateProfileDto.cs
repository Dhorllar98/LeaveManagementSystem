namespace LeaveManagement.Application.DTOs.Profile;

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public Guid? DepartmentId { get; set; } 
    public string? Designation { get; set; }
}