namespace LeaveManagement.Application.Interfaces;

public interface ILeaveResetService
{
    Task<(bool Success, string Message, int ProcessedCount)> ResetAnnualLeaveBalancesAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
}