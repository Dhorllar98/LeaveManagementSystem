using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!await context.LeaveTypes.AnyAsync())
        {
            var leaveTypes = new List<LeaveType>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Annual Leave",
                    DefaultDays = 20,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Sick Leave",
                    DefaultDays = 10,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Maternity/Paternity Leave",
                    DefaultDays = 30,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.LeaveTypes.AddRangeAsync(leaveTypes);
        }

        if (!await context.Users.AnyAsync())
        {
            string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Passw0rd123!");

            var defaultUsers = new List<User>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Admin HR Manager",
                    Email = "admin@admin.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.Manager,
                    LeaveBalance = 20,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "John Doe",
                    Email = "john.doe@user.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.Employee,
                    LeaveBalance = 20,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(defaultUsers);
        }

        await context.SaveChangesAsync();
    }
}