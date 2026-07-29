using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(l => l.ManagerComments)
            .HasMaxLength(500);

        builder.HasOne(l => l.Employee)
            .WithMany(u => u.LeaveRequests)
            .HasForeignKey(l => l.EmployeeId);
    }
}