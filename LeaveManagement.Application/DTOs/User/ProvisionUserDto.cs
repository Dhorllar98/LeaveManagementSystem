namespace LeaveManagement.Application.DTOs.User;

public class ProvisionUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? TeamLeadId { get; set; }
    public string ResetPasswordUrl { get; set; } = "https://new-leave-management-system-qszg.vercel.app/reset-token";
}