using LeaveManagement.Domain.Enums;

namespace LeaveManagement.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Employee;
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public int LeaveBalance { get; set; } = 20; // Default 20 days per year
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation collection
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
