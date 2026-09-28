using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Enforce unique department names per organization
        builder.HasIndex(d => new { d.OrganizationId, d.Name })
            .IsUnique();

        // Foreign Key: Organization -> Department (Cascade Delete)
        builder.HasOne(d => d.Organization)
            .WithMany()
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign Key: TeamLead -> User (Restrict Delete to prevent cycles)
        builder.HasOne(d => d.TeamLead)
            .WithMany()
            .HasForeignKey(d => d.TeamLeadId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: Department -> Employees (User.DepartmentId set to null on delete)
        builder.HasMany(d => d.Employees)
            .WithOne(u => u.Department)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}