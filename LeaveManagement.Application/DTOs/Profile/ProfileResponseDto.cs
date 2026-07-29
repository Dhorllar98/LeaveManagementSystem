using LeaveManagement.Domain.Enums;

namespace LeaveManagement.Application.DTOs.Profile;

public class ProfileResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int LeaveBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}