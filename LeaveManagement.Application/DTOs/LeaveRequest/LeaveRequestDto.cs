namespace LeaveManagement.Application.DTOs.LeaveRequest;

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public bool? Approved { get; set; } // null = Pending, true = Approved, false = Rejected
    public bool Cancelled { get; set; }
    public string? RequestComments { get; set; }
}