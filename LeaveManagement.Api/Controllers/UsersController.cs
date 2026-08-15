using System.Security.Claims;
using LeaveManagement.Application.DTOs.Auth;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : BaseController
{
    private readonly AppDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private async Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(currentUserIdStr, out var currentUserId)) return null;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        return user?.OrganizationId;
    }

    [HttpGet]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var orgId = await GetOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "User organization not found." });

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == orgId)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.EmployeeCode,
                u.OrganizationId,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                TeamLeadId = u.TeamLeadId,
                TeamLeadName = u.TeamLead != null ? u.TeamLead.FullName : null,
                u.Designation,
                Role = u.Role.ToString(),
                u.LeaveBalance,
                u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "User organization not found." });

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id && u.OrganizationId == orgId)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.EmployeeCode,
                u.OrganizationId,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                TeamLeadId = u.TeamLeadId,
                TeamLeadName = u.TeamLead != null ? u.TeamLead.FullName : null,
                u.Designation,
                Role = u.Role.ToString(),
                u.LeaveBalance,
                u.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{id}' not found or does not belong to your organization." });
        }

        return Ok(user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "User organization not found." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == orgId, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{id}' not found or does not belong to your organization." });
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName;
        if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Designation)) user.Designation = dto.Designation;
        if (dto.DepartmentId.HasValue) user.DepartmentId = dto.DepartmentId;
        if (dto.TeamLeadId.HasValue) user.TeamLeadId = dto.TeamLeadId;
        if (dto.LeaveBalance.HasValue) user.LeaveBalance = dto.LeaveBalance.Value;

        if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole))
        {
            user.Role = parsedRole;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Employee updated successfully.",
            data = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.EmployeeCode,
                user.DepartmentId,
                user.TeamLeadId,
                user.Designation,
                Role = user.Role.ToString(),
                user.LeaveBalance
            }
        });
    }

    [HttpPost("bulk-upload")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> BulkUploadUsers(
        IFormFile file,
        [FromServices] IEmailService emailService,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please upload a valid CSV file." });

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .csv files are supported." });

        var hrUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(hrUserIdStr, out var hrUserId))
        {
            return Unauthorized(new { message = "User identity invalid." });
        }

        var hrUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == hrUserId, cancellationToken);
        if (hrUser?.OrganizationId == null)
        {
            return BadRequest(new { message = "HR account is not linked to any organization." });
        }

        var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == hrUser.OrganizationId, cancellationToken);
        if (org == null)
        {
            return BadRequest(new { message = "Organization not found." });
        }

        var departments = await _context.Departments
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var existingEmails = await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == org.Id)
            .Select(u => u.Email.ToLower())
            .ToListAsync(cancellationToken);

        var emailHashSet = new HashSet<string>(existingEmails);

        var createdUsers = new List<User>();
        var emailTasks = new List<Task>();
        var errors = new List<string>();
        int rowNumber = 1;

        using var reader = new StreamReader(file.OpenReadStream());
        await reader.ReadLineAsync(cancellationToken);

        while (!reader.EndOfStream)
        {
            rowNumber++;
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 2)
            {
                errors.Add($"Row {rowNumber}: Minimum required fields are FullName and Email.");
                continue;
            }

            string fullName = cols[0].Trim();
            string email = cols[1].Trim();
            string roleStr = cols.Length > 2 ? cols[2].Trim() : "Employee";
            string designation = cols.Length > 3 ? cols[3].Trim() : "Employee";
            string deptName = cols.Length > 4 ? cols[4].Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                errors.Add($"Row {rowNumber}: FullName and Email cannot be empty.");
                continue;
            }

            if (emailHashSet.Contains(email.ToLower()))
            {
                errors.Add($"Row {rowNumber}: Email '{email}' already exists in your organization.");
                continue;
            }

            Guid? deptId = null;
            if (!string.IsNullOrWhiteSpace(deptName))
            {
                var matchedDept = departments.FirstOrDefault(d => d.Name.Equals(deptName, StringComparison.OrdinalIgnoreCase));
                if (matchedDept != null) deptId = matchedDept.Id;
            }

            org.LastEmployeeNumber++;
            string formattedCode = $"{org.CodePrefix}-{org.LastEmployeeNumber:D2}";

            string tempPassword = "Welcome" + Random.Shared.Next(1000, 9999) + "!";
            Enum.TryParse<UserRole>(roleStr, true, out var userRole);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                Role = userRole,
                Designation = designation,
                DepartmentId = deptId,
                OrganizationId = org.Id,
                EmployeeCode = formattedCode,
                LeaveBalance = 20,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(newUser, cancellationToken);
            createdUsers.Add(newUser);
            emailHashSet.Add(email.ToLower());

            string emailBody = $@"
                <h3>Welcome to LeaveFlow, {fullName}!</h3>
                <p>An account has been created for you by HR.</p>
                <p><strong>Employee ID:</strong> {formattedCode}</p>
                <p><strong>Temporary Password:</strong> <code>{tempPassword}</code></p>";

            emailTasks.Add(emailService.SendEmailAsync(email, "Welcome to LeaveFlow", emailBody));
        }

        if (createdUsers.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _ = Task.WhenAll(emailTasks);
        }

        return Ok(new
        {
            success = true,
            message = $"Bulk upload completed. {createdUsers.Count} employee(s) created.",
            totalProcessed = rowNumber - 1,
            successfullyCreated = createdUsers.Count,
            errors
        });
    }
}