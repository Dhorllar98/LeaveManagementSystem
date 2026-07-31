using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public class LeaveRepository : ILeaveRepository
{
    private readonly AppDbContext _context;

    public LeaveRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<LeaveRequest> Items, int TotalCount)> GetPaginatedLeavesAsync(
        Guid? employeeId,
        LeaveStatus? status,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRequests
            .Include(l => l.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(l => l.EmployeeId == employeeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(l => (l.Reason != null && l.Reason.ToLower().Contains(term)) ||
                                     (l.Employee != null && l.Employee.FullName.ToLower().Contains(term)));
        }

        query = sortBy?.ToLower() switch
        {
            "date" => isAscending ? query.OrderBy(l => l.StartDate) : query.OrderByDescending(l => l.StartDate),
            "employee" => isAscending
                ? query.OrderBy(l => l.Employee != null ? l.Employee.FullName : string.Empty)
                : query.OrderByDescending(l => l.Employee != null ? l.Employee.FullName : string.Empty),
            "status" => isAscending ? query.OrderBy(l => l.Status) : query.OrderByDescending(l => l.Status),
            _ => query.OrderByDescending(l => l.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
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