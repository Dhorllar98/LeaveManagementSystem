using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Designation)
            .HasMaxLength(100);

        builder.Property(u => u.EmployeeCode)
            .HasMaxLength(50);

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(128);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.LeaveBalance)
            .HasDefaultValue(20);

        // Tenant-scoped unique indexes
        builder.HasIndex(u => new { u.OrganizationId, u.Email })
            .IsUnique();

        builder.HasIndex(u => new { u.OrganizationId, u.EmployeeCode })
            .IsUnique();

        // Organization Navigation (EXPLICIT MAPPING FIXED HERE)
        builder.HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationships
        builder.HasMany(u => u.LeaveRequests)
            .WithOne(l => l.Employee)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.TeamLead)
            .WithMany(u => u.Subordinates)
            .HasForeignKey(u => u.TeamLeadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}