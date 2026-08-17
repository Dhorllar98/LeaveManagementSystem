namespace LeaveManagement.Application.DTOs.Dashboard;

public class DashboardResponseDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string FullName => EmployeeName;
    public string Name => EmployeeName;

    public int PendingRequestsCount { get; set; }
    public int PendingRequests => PendingRequestsCount;
    public int PendingCount => PendingRequestsCount;

    public int ApprovedLeavesCount { get; set; }
    public int ApprovedLeaves => ApprovedLeavesCount;
    public int ApprovedCount => ApprovedLeavesCount;

    public int RejectedLeavesCount { get; set; }
    public int RejectedLeaves => RejectedLeavesCount;
    public int RejectedCount => RejectedLeavesCount;

    public int TotalLeaveDaysRemaining { get; set; }
    public int LeaveBalance => TotalLeaveDaysRemaining;
    public int RemainingDays => TotalLeaveDaysRemaining;
}

public class AdminDashboardResponseDto
{
    public int TotalEmployees { get; set; }
    public int EmployeeCount => TotalEmployees;

    public int TotalRequestsCount { get; set; }
    public int TotalRequests => TotalRequestsCount;

    public int PendingApprovalsCount { get; set; }
    public int PendingApprovals => PendingApprovalsCount;
    public int PendingRequestsCount => PendingApprovalsCount;
    public int PendingCount => PendingApprovalsCount;

    public int ApprovedRequestsCount { get; set; }
    public int ApprovedRequests => ApprovedRequestsCount;
    public int ApprovedCount => ApprovedRequestsCount;

    public int RejectedRequestsCount { get; set; }
    public int RejectedRequests => RejectedRequestsCount;
    public int RejectedCount => RejectedRequestsCount;

    public int EmployeesCurrentlyOnLeave { get; set; }
    public int EmployeesOnLeave => EmployeesCurrentlyOnLeave;
    public int OnLeaveCount => EmployeesCurrentlyOnLeave;
}