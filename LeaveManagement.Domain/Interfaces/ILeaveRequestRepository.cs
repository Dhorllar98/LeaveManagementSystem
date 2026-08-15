using LeaveManagement.Domain.Entities;

namespace LeaveManagement.Domain.Interfaces;

public interface ILeaveRequestRepository
{
    Task<IEnumerable<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequest>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    // FIX: Added organizationId to enforce tenant isolation at the database level
    Task<(IEnumerable<LeaveRequest> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId,
        Guid? employeeId,
        string? status,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default);
    void Update(LeaveRequest leaveRequest);
    void Delete(LeaveRequest leaveRequest);
}