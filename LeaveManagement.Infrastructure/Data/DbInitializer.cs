using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        // 1. Seed Default Organization (SBSC NIG)
        var defaultOrg = await context.Organizations.FirstOrDefaultAsync(o => o.CodePrefix == "SBSC-NIG");
        if (defaultOrg == null)
        {
            defaultOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "SBSC NIG",
                CodePrefix = "SBSC-NIG",
                LastEmployeeNumber = 3, 
                CreatedAt = DateTime.UtcNow
            };
            await context.Organizations.AddAsync(defaultOrg);
            await context.SaveChangesAsync();
        }

        // Leave Types
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

        // Default Departments
        Department? hrDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Human Resources");
        Department? engDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Engineering");

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

        // 4. Update Existing Users OR Add New Ones
        string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Passw0rd123!");

        var hrUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "hr@admin.com");
        var leadUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "teamlead@company.com");
        var empUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@user.com");

        if (hrUser != null)
        {
            hrUser.DepartmentId = hrDept.Id;
            hrUser.Designation = "HR Manager";
            hrUser.OrganizationId = defaultOrg.Id;
            hrUser.EmployeeCode = "SBSC-NIG-01";
        }

        if (leadUser != null)
        {
            leadUser.DepartmentId = engDept.Id;
            leadUser.Designation = "Lead Engineer";
            leadUser.OrganizationId = defaultOrg.Id;
            leadUser.EmployeeCode = "SBSC-NIG-02";
        }

        if (empUser != null)
        {
            empUser.DepartmentId = engDept.Id;
            empUser.Designation = "Backend Engineer";
            empUser.OrganizationId = defaultOrg.Id;
            empUser.EmployeeCode = "SBSC-NIG-03";
        }

        if (!await context.Users.AnyAsync())
        {
            var defaultUsers = new List<User>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Admin HR Manager",
                    Email = "hr@admin.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.HR,
                    DepartmentId = hrDept.Id,
                    Designation = "HR Manager",
                    OrganizationId = defaultOrg.Id,
                    EmployeeCode = "SBSC-NIG-01",
                    LeaveBalance = 20,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    FullName = "Sarah Jenkins",
                    Email = "teamlead@company.com",
                    PasswordHash = defaultPasswordHash,
                    Role = UserRole.TeamLead,
                    DepartmentId = engDept.Id,
                    Designation = "Lead Engineer",
                    OrganizationId = defaultOrg.Id,
                    EmployeeCode = "SBSC-NIG-02",
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
                    DepartmentId = engDept.Id,
                    Designation = "Backend Engineer",
                    OrganizationId = defaultOrg.Id,
                    EmployeeCode = "SBSC-NIG-03",
                    LeaveBalance = 20,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(defaultUsers);
        }

        await context.SaveChangesAsync();
    }
}