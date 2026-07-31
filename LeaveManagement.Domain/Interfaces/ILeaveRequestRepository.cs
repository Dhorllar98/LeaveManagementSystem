using LeaveManagement.Domain.Entities;

namespace LeaveManagement.Domain.Interfaces;

public interface ILeaveRequestRepository
{
    Task<IEnumerable<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    // Paginated, Filtered & Search Query (For Front-end easy access) 
    Task<(IEnumerable<LeaveRequest> Items, int TotalCount)> GetPagedAsync(
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