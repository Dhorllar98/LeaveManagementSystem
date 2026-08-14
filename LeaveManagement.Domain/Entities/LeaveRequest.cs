using LeaveManagement.Domain.Enums;

namespace LeaveManagement.Domain.Entities;

public class LeaveRequest
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;

    public int NumberOfDays { get; set; }
    public bool? Approved { get; set; }
    public LeaveStatus Status { get; set; }
    public bool Cancelled { get; set; }
    public string? RequestComments { get; set; }
    public string? Reason { get; set; }
    public string? ManagerComments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}