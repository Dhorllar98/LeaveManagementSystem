using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LeaveManagement.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveAllocation> LeaveAllocations => Set<LeaveAllocation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private List<AuditLog> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditLog = new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                Timestamp = DateTime.UtcNow
            };

            var changes = new Dictionary<string, object>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    auditLog.EntityId = property.CurrentValue?.ToString() ?? string.Empty;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        changes[property.Metadata.Name] = property.CurrentValue ?? "null";
                        break;
                    case EntityState.Deleted:
                        changes[property.Metadata.Name] = property.OriginalValue ?? "null";
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            changes[property.Metadata.Name] = new
                            {
                                Old = property.OriginalValue,
                                New = property.CurrentValue
                            };
                        }
                        break;
                }
            }

            auditLog.Changes = JsonSerializer.Serialize(changes);
            auditEntries.Add(auditLog);
        }

        return auditEntries;
    }
}