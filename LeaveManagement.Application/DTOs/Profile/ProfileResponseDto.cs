namespace LeaveManagement.Application.DTOs.Profile;

public class ProfileResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Name => FullName; 
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? DepartmentName { get; set; } 
    public string? Designation { get; set; }
    public string? JobTitle => Designation; 
    public string? EmployeeCode { get; set; }
    public string? EmployeeId { get; set; } 
    public int LeaveBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? OrganizationId { get; set; }
}