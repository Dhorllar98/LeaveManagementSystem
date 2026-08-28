using LeaveManagement.Application.DTOs.LeaveAllocation;

namespace LeaveManagement.Application.Interfaces;

public interface ILeaveAllocationService
{
    Task<IEnumerable<LeaveAllocationDto>> GetAllocationsByOrganizationAsync(Guid orgId, CancellationToken cancellationToken = default);
    Task<LeaveAllocationDto?> GetAllocationByIdAsync(Guid id, Guid orgId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? ErrorMessage, LeaveAllocationDto? Data)> CreateAllocationAsync(CreateLeaveAllocationDto dto, Guid orgId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAllocationAsync(Guid id, UpdateLeaveAllocationDto dto, Guid orgId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAllocationAsync(Guid id, Guid orgId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserLeaveBalanceDto>> GetUserLeaveBalancesAsync(Guid userId, Guid orgId, int period, CancellationToken cancellationToken = default);
}