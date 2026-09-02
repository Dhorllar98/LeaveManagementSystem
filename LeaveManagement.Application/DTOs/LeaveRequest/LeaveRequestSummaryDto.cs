namespace LeaveManagement.Application.DTOs.LeaveRequest;

public class LeaveTypeSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class LeaveRequestSummaryDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;

    public LeaveTypeSummaryDto? LeaveType { get; set; }

    public Guid? HandoverUserId { get; set; }
    public string? HandoverUserName { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ManagerComments { get; set; }
    public DateTime CreatedAt { get; set; }
}