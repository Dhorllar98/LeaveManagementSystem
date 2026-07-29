namespace LeaveManagement.Application.DTOs.Dashboard;

public class DashboardResponseDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public int PendingRequestsCount { get; set; }
    public int ApprovedLeavesCount { get; set; }
    public int RejectedLeavesCount { get; set; }
    public int TotalLeaveDaysRemaining { get; set; }
}