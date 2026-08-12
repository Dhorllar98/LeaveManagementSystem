using LeaveManagement.Domain.Enums;

namespace LeaveManagement.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    public string? Designation { get; set; }
    public UserRole Role { get; set; } = UserRole.Employee;
}