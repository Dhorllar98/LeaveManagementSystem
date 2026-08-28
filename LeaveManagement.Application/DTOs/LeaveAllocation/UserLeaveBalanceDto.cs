namespace LeaveManagement.Application.DTOs.LeaveAllocation;

public class UserLeaveBalanceDto
{
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int DaysUsed { get; set; }
    public int DaysRemaining => TotalDays - DaysUsed;
}