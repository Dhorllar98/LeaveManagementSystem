using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;

    public DepartmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Department?> GetByIdAsync(Guid id)
    {
        return await _context.Departments
            .Include(d => d.TeamLead)
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        return await _context.Departments
            .Include(d => d.TeamLead)
            .Include(d => d.Employees)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Department>> GetAllByOrganizationAsync(Guid organizationId)
    {
        return await _context.Departments
            .Include(d => d.TeamLead)
            .Include(d => d.Employees)
            .Where(d => d.OrganizationId == organizationId || d.OrganizationId == Guid.Empty)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid organizationId)
    {
        return await _context.Departments
            .AnyAsync(d => (d.OrganizationId == organizationId || d.OrganizationId == Guid.Empty) && d.Name.ToLower() == name.ToLower());
    }

    public async Task AddAsync(Department department)
    {
        await _context.Departments.AddAsync(department);
    }

    public async Task UpdateAsync(Department department)
    {
        _context.Departments.Update(department);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}