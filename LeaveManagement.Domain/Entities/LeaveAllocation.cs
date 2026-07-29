namespace LeaveManagement.Domain.Entities;

public class LeaveAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int NumberOfDays { get; set; }

    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;

    public int Period { get; set; } // e.g., Year like 2026
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}