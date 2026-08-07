namespace LeaveManagement.Application.DTOs.Auth;

public class AdminCreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee"; 
    public string ClientResetUrl { get; set; } = string.Empty;
}