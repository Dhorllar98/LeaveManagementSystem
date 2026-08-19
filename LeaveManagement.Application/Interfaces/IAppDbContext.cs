using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<Department> Departments { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveAllocation> LeaveAllocations { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<NotificationSetting> NotificationSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}