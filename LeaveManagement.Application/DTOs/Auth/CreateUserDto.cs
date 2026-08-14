namespace LeaveManagement.Application.DTOs.Auth;

public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }
    public Guid? TeamLeadId { get; set; }

    public string Designation { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee";
    public string ClientResetUrl { get; set; } = string.Empty;
}