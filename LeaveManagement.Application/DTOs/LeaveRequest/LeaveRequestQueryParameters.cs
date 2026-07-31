namespace LeaveManagement.Application.DTOs.LeaveRequest;

public class LeaveRequestQueryParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }        // e.g., "Pending", "Approved", "Rejected"
    public string? SearchTerm { get; set; }    // Search employee name or reason
    public Guid? EmployeeId { get; set; }      // Filter by specific employee
}