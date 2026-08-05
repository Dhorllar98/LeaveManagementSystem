namespace LeaveManagement.Application.DTOs.Dashboard;

public class DashboardResponseDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public int PendingRequestsCount { get; set; }
    public int ApprovedLeavesCount { get; set; }
    public int RejectedLeavesCount { get; set; }
    public int TotalLeaveDaysRemaining { get; set; }
}

public class AdminDashboardResponseDto
{
    public int TotalEmployees { get; set; }
    public int PendingApprovalsCount { get; set; }
    public int ApprovedRequestsCount { get; set; }
    public int RejectedRequestsCount { get; set; }
    public int EmployeesCurrentlyOnLeave { get; set; }
}