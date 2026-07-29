using LeaveManagement.Domain.Entities;

namespace LeaveManagement.Domain.Interfaces;

public interface ILeaveAllocationRepository
{
    Task<IEnumerable<LeaveAllocation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LeaveAllocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveAllocation>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveAllocation leaveAllocation, CancellationToken cancellationToken = default);
    void Update(LeaveAllocation leaveAllocation);
    void Delete(LeaveAllocation leaveAllocation);
    Task<bool> AllocationExistsAsync(Guid employeeId, Guid leaveTypeId, int period, CancellationToken cancellationToken = default);
}