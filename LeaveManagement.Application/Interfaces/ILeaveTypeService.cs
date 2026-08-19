using LeaveManagement.Application.DTOs.LeaveType;

namespace LeaveManagement.Application.Interfaces;

public interface ILeaveTypeService
{
    Task<IEnumerable<LeaveTypeDto>> GetLeaveTypesAsync(Guid orgId, CancellationToken cancellationToken = default);
    Task<LeaveTypeDto?> GetLeaveTypeByIdAsync(Guid id, Guid orgId, CancellationToken cancellationToken = default);
    Task<LeaveTypeDto> CreateLeaveTypeAsync(CreateLeaveTypeDto dto, Guid orgId, CancellationToken cancellationToken = default);
    Task<bool> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeDto dto, Guid orgId, CancellationToken cancellationToken = default);
    Task<bool> DeleteLeaveTypeAsync(Guid id, Guid orgId, CancellationToken cancellationToken = default);
}