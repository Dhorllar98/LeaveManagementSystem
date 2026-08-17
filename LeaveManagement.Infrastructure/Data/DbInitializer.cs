using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        //Seed Default Organization
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
                new() { Id = Guid.NewGuid(), Name = "Annual Leave", DefaultDays = 20, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "Sick Leave", DefaultDays = 10, CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Name = "Maternity/Paternity Leave", DefaultDays = 30, CreatedAt = DateTime.UtcNow }
            };

            await context.LeaveTypes.AddRangeAsync(leaveTypes);
            await context.SaveChangesAsync();
        }

        // Default Departments
        Department hrDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Human Resources")
                            ?? new Department { Id = Guid.NewGuid(), Name = "Human Resources", CreatedAt = DateTime.UtcNow };

        Department engDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Engineering")
                             ?? new Department { Id = Guid.NewGuid(), Name = "Engineering", CreatedAt = DateTime.UtcNow };

        if (context.Entry(hrDept).State == EntityState.Detached) await context.Departments.AddAsync(hrDept);
        if (context.Entry(engDept).State == EntityState.Detached) await context.Departments.AddAsync(engDept);

        await context.SaveChangesAsync();

        //(Guarantees creation AND valid password hashes)
        string defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Passw0rd123!");

        // HR User
        var hrUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "hr@admin.com");
        if (hrUser == null)
        {
            hrUser = new User { Id = Guid.NewGuid(), Email = "hr@admin.com", FullName = "Admin HR Manager", Role = UserRole.HR, CreatedAt = DateTime.UtcNow };
            await context.Users.AddAsync(hrUser);
        }
        hrUser.PasswordHash = defaultPasswordHash;
        hrUser.DepartmentId = hrDept.Id;
        hrUser.Designation = "HR Manager";
        hrUser.OrganizationId = defaultOrg.Id;
        hrUser.EmployeeCode = "SBSC-NIG-01";
        hrUser.LeaveBalance = 20;

        // Team Lead User
        var leadUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "teamlead@company.com");
        if (leadUser == null)
        {
            leadUser = new User { Id = Guid.NewGuid(), Email = "teamlead@company.com", FullName = "Sarah Jenkins", Role = UserRole.TeamLead, CreatedAt = DateTime.UtcNow };
            await context.Users.AddAsync(leadUser);
        }
        leadUser.PasswordHash = defaultPasswordHash;
        leadUser.DepartmentId = engDept.Id;
        leadUser.Designation = "Lead Engineer";
        leadUser.OrganizationId = defaultOrg.Id;
        leadUser.EmployeeCode = "SBSC-NIG-02";
        leadUser.LeaveBalance = 20;

        // Employee User
        var empUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "john.doe@user.com");
        if (empUser == null)
        {
            empUser = new User { Id = Guid.NewGuid(), Email = "john.doe@user.com", FullName = "John Doe", Role = UserRole.Employee, CreatedAt = DateTime.UtcNow };
            await context.Users.AddAsync(empUser);
        }
        empUser.PasswordHash = defaultPasswordHash;
        empUser.DepartmentId = engDept.Id;
        empUser.Designation = "Backend Engineer";
        empUser.OrganizationId = defaultOrg.Id;
        empUser.EmployeeCode = "SBSC-NIG-03";
        empUser.LeaveBalance = 20;

        await context.SaveChangesAsync();
    }
}