using LeaveManagement.Domain.Entities;
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