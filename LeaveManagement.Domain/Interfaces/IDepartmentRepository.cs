using LeaveManagement.Domain.Entities;

namespace LeaveManagement.Domain.Interfaces;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id);
    Task<IEnumerable<Department>> GetAllAsync();
    Task<bool> ExistsByNameAsync(string name);
    Task AddAsync(Department department);
    Task UpdateAsync(Department department);
    Task SaveChangesAsync();
}