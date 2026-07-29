using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;

namespace LeaveManagement.Domain.Interfaces;

public interface ILeaveRepository
{
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<(IEnumerable<LeaveRequest> Items, int TotalCount)> GetPaginatedLeavesAsync(
        Guid? employeeId,
        LeaveStatus? status,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
    void Update(LeaveRequest leaveRequest);
    void Delete(LeaveRequest leaveRequest);
}