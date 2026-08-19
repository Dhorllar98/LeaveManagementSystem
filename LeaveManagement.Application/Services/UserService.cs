using LeaveManagement.Application.DTOs.User;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;

    public UserService(IAppDbContext context, IEmailService emailService, ILogger<UserService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    private async Task<Guid?> GetOrganizationIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user?.OrganizationId;
    }

    public async Task<IEnumerable<UserResponseDto>?> GetUsersAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var orgId = await GetOrganizationIdAsync(currentUserId, cancellationToken);
        if (orgId == null) return null;

        return await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == orgId)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                EmployeeCode = u.EmployeeCode,
                OrganizationId = u.OrganizationId,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                TeamLeadId = u.TeamLeadId,
                TeamLeadName = u.TeamLead != null ? u.TeamLead.FullName : null,
                Designation = u.Designation,
                Role = u.Role.ToString(),
                LeaveBalance = u.LeaveBalance,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var orgId = await GetOrganizationIdAsync(currentUserId, cancellationToken);
        if (orgId == null) return null;

        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == id && u.OrganizationId == orgId)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                EmployeeCode = u.EmployeeCode,
                OrganizationId = u.OrganizationId,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                TeamLeadId = u.TeamLeadId,
                TeamLeadName = u.TeamLead != null ? u.TeamLead.FullName : null,
                Designation = u.Designation,
                Role = u.Role.ToString(),
                LeaveBalance = u.LeaveBalance,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message, int StatusCode, object? Data)> ProvisionUserAsync(
        Guid hrUserId,
        ProvisionUserDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return (false, "FullName and Email are required.", 400, null);
        }

        var hrUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == hrUserId, cancellationToken);
        if (hrUser?.OrganizationId == null)
        {
            return (false, "HR account is not linked to any organization.", 400, null);
        }

        var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == hrUser.OrganizationId, cancellationToken);
        if (org == null)
        {
            return (false, "Organization not found.", 400, null);
        }

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.OrganizationId == org.Id && u.Email.ToLower() == dto.Email.ToLower(), cancellationToken);

        if (emailExists)
        {
            return (false, $"Email '{dto.Email}' already exists in your organization.", 400, null);
        }

        org.LastEmployeeNumber++;
        string formattedCode = $"{org.CodePrefix}-{org.LastEmployeeNumber:D2}";
        string tempPassword = "Welcome" + Random.Shared.Next(1000, 9999) + "!";
        string resetToken = Guid.NewGuid().ToString("N");

        Enum.TryParse<UserRole>(dto.Role, true, out var userRole);

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            Role = userRole,
            Designation = string.IsNullOrWhiteSpace(dto.Designation) ? "Employee" : dto.Designation.Trim(),
            DepartmentId = dto.DepartmentId,
            TeamLeadId = dto.TeamLeadId,
            OrganizationId = org.Id,
            EmployeeCode = formattedCode,
            LeaveBalance = 20,
            PasswordResetToken = resetToken, 
            ResetTokenExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(newUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Build reset link
        string baseUrl = string.IsNullOrWhiteSpace(dto.ResetPasswordUrl)
            ? "https://new-leave-management-system-qszg.vercel.app/reset-password"
            : dto.ResetPasswordUrl.TrimEnd('/');

        string resetLink = $"{baseUrl}?token={resetToken}&email={Uri.EscapeDataString(newUser.Email)}";

        // Clickable HTML Email Body
        string emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333;'>
                <h3>Welcome to LeaveFlow, {dto.FullName}!</h3>
                <p>An account has been created for you by HR.</p>
                <p><strong>Employee ID:</strong> {formattedCode}</p>
                <p><strong>Temporary Password:</strong> <code style='background: #f4f4f4; padding: 2px 6px; border-radius: 4px;'>{tempPassword}</code></p>
                
                <p style='margin-top: 25px;'>Please click the button below to complete your setup and create a new password:</p>
                
                <div style='margin: 20px 0;'>
                    <a href='{resetLink}' target='_blank' style='background-color: #007bff; color: #ffffff; padding: 12px 20px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Set Permanent Password</a>
                </div>

                <p style='font-size: 13px; color: #666;'>If the button above does not work, click or copy this direct link:<br/>
                    <a href='{resetLink}' target='_blank' style='color: #007bff;'>{resetLink}</a>
                </p>

                <p style='font-size: 12px; color: #888; margin-top: 30px;'><em>Note: This password reset link will expire in 24 hours.</em></p>
            </div>";

        _ = _emailService.SendEmailAsync(dto.Email, "Welcome to LeaveFlow", emailBody);

        var resultData = new
        {
            newUser.Id,
            newUser.FullName,
            newUser.Email,
            newUser.EmployeeCode,
            newUser.DepartmentId,
            newUser.TeamLeadId,
            newUser.Designation,
            Role = newUser.Role.ToString(),
            newUser.LeaveBalance
        };

        return (true, "User provisioned successfully.", 201, resultData);
    }

    public async Task<(bool Success, string Message, int StatusCode, object? Data)> UpdateUserAsync(
        Guid id,
        Guid currentUserId,
        UpdateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetOrganizationIdAsync(currentUserId, cancellationToken);
        if (orgId == null) return (false, "User organization not found.", 400, null);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == orgId, cancellationToken);
        if (user == null)
        {
            return (false, $"User with ID '{id}' not found or does not belong to your organization.", 404, null);
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

        var updatedData = new
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
        };

        return (true, "Employee updated successfully.", 200, updatedData);
    }

    public async Task<(bool Success, string Message, int StatusCode, BulkUploadResultDto? Data)> BulkUploadUsersAsync(
        Guid hrUserId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return (false, "Please upload a valid CSV file.", 400, null);

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return (false, "Only .csv files are supported.", 400, null);

        var hrUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == hrUserId, cancellationToken);
        if (hrUser?.OrganizationId == null)
        {
            return (false, "HR account is not linked to any organization.", 400, null);
        }

        var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == hrUser.OrganizationId, cancellationToken);
        if (org == null)
        {
            return (false, "Organization not found.", 400, null);
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

            emailTasks.Add(_emailService.SendEmailAsync(email, "Welcome to LeaveFlow", emailBody));
        }

        if (createdUsers.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _ = Task.WhenAll(emailTasks);
        }

        var result = new BulkUploadResultDto
        {
            Success = true,
            Message = $"Bulk upload completed. {createdUsers.Count} employee(s) created.",
            TotalProcessed = rowNumber - 1,
            SuccessfullyCreated = createdUsers.Count,
            Errors = errors
        };

        return (true, result.Message, 200, result);
    }
}