using LeaveManagement.Domain.Enums;

namespace LeaveManagement.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? EmployeeCode { get; set; } // e.g. "SBSC-NIG-01"
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Employee;
    public string? Designation { get; set; }
    public int LeaveBalance { get; set; } = 20;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? TeamLeadId { get; set; }
    public User? TeamLead { get; set; }

    public ICollection<User> Subordinates { get; set; } = new List<User>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}