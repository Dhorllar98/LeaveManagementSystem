using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly AppDbContext _context;

    public LeaveTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken = default)
    {
        await _context.LeaveTypes.AddAsync(leaveType, cancellationToken);
    }

    public void Update(LeaveType leaveType)
    {
        _context.LeaveTypes.Update(leaveType);
    }

    public void Delete(LeaveType leaveType)
    {
        _context.LeaveTypes.Remove(leaveType);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes.AnyAsync(e => e.Id == id, cancellationToken);
    }
}