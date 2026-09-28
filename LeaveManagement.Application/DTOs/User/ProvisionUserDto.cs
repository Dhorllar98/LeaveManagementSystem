namespace LeaveManagement.Application.DTOs.User;

public class ProvisionUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string Role { get; set; } = "Employee";
    public string? Designation { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? TeamLeadId { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ResetPasswordUrl { get; set; } = "https://new-leave-management-system-qszg.vercel.app/reset-password";
}