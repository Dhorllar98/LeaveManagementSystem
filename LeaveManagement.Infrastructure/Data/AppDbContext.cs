using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LeaveManagement.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveAllocation> LeaveAllocations => Set<LeaveAllocation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaveChanges();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditLogs = new List<AuditLog>();
        var currentUserId = _currentUserService?.UserId?.ToString();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            // Automatically manage entity audit timestamps
            UpdateEntityAuditProperties(entry, now);

            // Build structural change audit
            var auditLog = new AuditLog
            {
                EntityName = entry.Metadata.ClrType.Name, // Proxy-safe class name
                Action = entry.State.ToString(),
                Timestamp = now,
                UserId = currentUserId
            };

            var changes = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    auditLog.EntityId = property.CurrentValue?.ToString() ?? string.Empty;
                    continue;
                }

                // Skip concurrency tokens from change payloads
                if (property.Metadata.IsConcurrencyToken)
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        changes[property.Metadata.Name] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        changes[property.Metadata.Name] = property.OriginalValue;
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
            auditLogs.Add(auditLog);
        }

        if (auditLogs.Count > 0)
        {
            AuditLogs.AddRange(auditLogs);
        }
    }

    private static void UpdateEntityAuditProperties(EntityEntry entry, DateTime now)
    {
        // Reflection/Property check for entities containing CreatedAt / UpdatedAt
        var createdAtProp = entry.Property("CreatedAt");
        var updatedAtProp = entry.Property("UpdatedAt");

        if (entry.State == EntityState.Added && createdAtProp != null)
        {
            createdAtProp.CurrentValue = now;
        }

        if (entry.State == EntityState.Modified && updatedAtProp != null)
        {
            updatedAtProp.CurrentValue = now;
        }
    }
}