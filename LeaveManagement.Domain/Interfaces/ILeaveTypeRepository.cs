using LeaveManagement.Domain.Entities;

namespace LeaveManagement.Domain.Interfaces;

public interface ILeaveTypeRepository
{
    Task<IEnumerable<LeaveType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken = default);
    void Update(LeaveType leaveType);
    void Delete(LeaveType leaveType);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}