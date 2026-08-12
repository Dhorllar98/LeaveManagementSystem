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

    public async Task<(IEnumerable<LeaveRequest> Items, int TotalCount)> GetPagedAsync(
        Guid? employeeId,
        string? status,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue && employeeId.Value != Guid.Empty)
        {
            query = query.Where(l => l.EmployeeId == employeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(l => l.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(l =>
                (l.Employee != null && l.Employee.FullName.ToLower().Contains(searchTerm.ToLower())) ||
                (l.Reason != null && l.Reason.ToLower().Contains(searchTerm.ToLower())));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LeaveRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveRequest entity, CancellationToken cancellationToken = default)
    {
        await _context.LeaveRequests.AddAsync(entity, cancellationToken);
    }

    public void Update(LeaveRequest entity)
    {
        _context.LeaveRequests.Update(entity);
    }

    public void Delete(LeaveRequest entity)
    {
        _context.LeaveRequests.Remove(entity);
    }
}