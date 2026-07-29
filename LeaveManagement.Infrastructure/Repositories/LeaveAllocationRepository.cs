using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public class LeaveAllocationRepository : ILeaveAllocationRepository
{
    private readonly AppDbContext _context;

    public LeaveAllocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveAllocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LeaveAllocations
            .Include(la => la.Employee)
            .Include(la => la.LeaveType)
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveAllocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveAllocations
            .Include(la => la.Employee)
            .Include(la => la.LeaveType)
            .FirstOrDefaultAsync(la => la.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<LeaveAllocation>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveAllocations
            .Where(la => la.EmployeeId == employeeId)
            .Include(la => la.Employee)
            .Include(la => la.LeaveType)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveAllocation leaveAllocation, CancellationToken cancellationToken = default)
    {
        await _context.LeaveAllocations.AddAsync(leaveAllocation, cancellationToken);
    }

    public void Update(LeaveAllocation leaveAllocation)
    {
        _context.LeaveAllocations.Update(leaveAllocation);
    }

    public void Delete(LeaveAllocation leaveAllocation)
    {
        _context.LeaveAllocations.Remove(leaveAllocation);
    }

    public async Task<bool> AllocationExistsAsync(Guid employeeId, Guid leaveTypeId, int period, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveAllocations
            .AnyAsync(la => la.EmployeeId == employeeId && la.LeaveTypeId == leaveTypeId && la.Period == period, cancellationToken);
    }
}