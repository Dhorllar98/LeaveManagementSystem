using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly AppDbContext _context;

    public LeaveRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Where(lr => lr.EmployeeId == employeeId)
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .ToListAsync(cancellationToken);
    }

    //PAGINATION, FILTERING & SEARCH IMPLEMENTATION (To support the Angular frontend's server-side pagination,
    //                  status filtering, RxJS search, and editing/deleting)
    public async Task<(IEnumerable<LeaveRequest> Items, int TotalCount)> GetPagedAsync(
        Guid? employeeId,
        string? status,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .AsNoTracking();

        // 1. Employee Filter
        if (employeeId.HasValue)
        {
            baseQuery = baseQuery.Where(lr => lr.EmployeeId == employeeId.Value);
        }

        // 2. Status Filter
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveStatus>(status, true, out var parsedStatus))
        {
            baseQuery = baseQuery.Where(lr => lr.Status == parsedStatus);
        }

        // 3. Search Term (Reason or Employee Full Name)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            baseQuery = baseQuery.Where(lr =>
                (lr.Reason != null && lr.Reason.ToLower().Contains(term)) ||
                (lr.Employee != null && lr.Employee.FullName != null && lr.Employee.FullName.ToLower().Contains(term)));
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(lr => lr.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        await _context.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
    }

    public void Update(LeaveRequest leaveRequest)
    {
        _context.LeaveRequests.Update(leaveRequest);
    }

    public void Delete(LeaveRequest leaveRequest)
    {
        _context.LeaveRequests.Remove(leaveRequest);
    }
}