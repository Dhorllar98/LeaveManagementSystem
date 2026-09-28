using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure; 

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
    DbSet<PublicHoliday> PublicHolidays { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // Expose the database facade for transactions & execution strategies
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}