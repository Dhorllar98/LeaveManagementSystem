namespace LeaveManagement.Application.DTOs.LeaveAllocation;

public class LeaveAllocationDto
{
    public Guid Id { get; set; }
    public int NumberOfDays { get; set; }
    public int Period { get; set; } // e.g., Year 2026
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
}