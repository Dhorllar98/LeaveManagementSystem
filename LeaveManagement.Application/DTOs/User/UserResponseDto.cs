using LeaveManagement.Application.DTOs.LeaveAllocation;

namespace LeaveManagement.Application.DTOs.User;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? TeamLeadId { get; set; }
    public string? TeamLeadName { get; set; }
    public string? Designation { get; set; }
    public string Role { get; set; } = string.Empty;
    public int LeaveBalance { get; set; }
    public DateTime CreatedAt { get; set; }

    // Detailed leave balance list per leave type
    public List<UserLeaveBalanceDto> LeaveBalances { get; set; } = new();
}