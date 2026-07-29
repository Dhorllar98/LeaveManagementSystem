using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Data.Configurations;

public class LeaveAllocationConfiguration : IEntityTypeConfiguration<LeaveAllocation>
{
    public void Configure(EntityTypeBuilder<LeaveAllocation> builder)
    {
        builder.ToTable("LeaveAllocations");

        builder.HasKey(la => la.Id);

        builder.Property(la => la.NumberOfDays)
            .IsRequired();

        builder.Property(la => la.Period)
            .IsRequired();

        // Explicitly map relationship to LeaveType to prevent shadow properties
        builder.HasOne(la => la.LeaveType)
            .WithMany()
            .HasForeignKey(la => la.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Explicitly map relationship to User (Employee)
        builder.HasOne(la => la.Employee)
            .WithMany()
            .HasForeignKey(la => la.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}