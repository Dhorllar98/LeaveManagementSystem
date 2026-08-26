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

        builder.Property(d => d.OrganizationId)
            .IsRequired();

        // Nullable foreign key: Departments start unassigned until HR explicitly assigns a team lead
        builder.HasOne(d => d.TeamLead)
            .WithMany()
            .HasForeignKey(d => d.TeamLeadId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}