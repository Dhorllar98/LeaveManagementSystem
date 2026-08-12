using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        // 1. Seed Leave Types
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
            await context.SaveChangesAsync();
        }

        // 2. Seed Default Departments
        Department? hrDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Human Resources");
        Department? engDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Engineering");

        if (hrDept == null || engDept == null)
        {
            if (hrDept == null)
            {
                hrDept = new Department { Id = Guid.NewGuid(), Name = "Human Resources", CreatedAt = DateTime.UtcNow };
                await context.Departments.AddAsync(hrDept);
            }

            if (engDept == null)
            {
                engDept = new Department { Id = Guid.NewGuid(), Name = "Engineering", CreatedAt = DateTime.UtcNow };
                await context.Departments.AddAsync(engDept);
            }

            await context.SaveChangesAsync();
        }

        // 3. Seed Default Users
        if (!await context.Users.AnyAsync())
        {
            string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Passw0rd123!");

            var defaultUsers = new List<User>
            {
                // 1. HR Role
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Admin HR Manager",
                    Email = "hr@admin.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.HR,
                    DepartmentId = hrDept.Id, 
                    Designation = "HR Manager",
                    LeaveBalance = 20,
                    CreatedAt = DateTime.UtcNow
                },

                // 2. Team Lead Role
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Sarah Jenkins",
                    Email = "teamlead@company.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.TeamLead,
                    DepartmentId = engDept.Id, 
                    Designation = "Lead Engineer",
                    LeaveBalance = 20,
                    CreatedAt = DateTime.UtcNow
                },

                // 3. Standard Employee Role
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "John Doe",
                    Email = "john.doe@user.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.Employee,
                    DepartmentId = engDept.Id, 
                    Designation = "Backend Engineer",
                    LeaveBalance = 20,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(defaultUsers);
            await context.SaveChangesAsync();
        }
    }
}