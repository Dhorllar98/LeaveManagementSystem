namespace LeaveManagement.Application.DTOs.LeaveRequest;

public class LeaveRequestFilterDto
{
    public Guid? EmployeeId { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public string? Department { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}