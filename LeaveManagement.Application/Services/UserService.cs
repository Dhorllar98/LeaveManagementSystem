using System.Globalization;
using ClosedXML.Excel;
using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.DTOs.LeaveAllocation;
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
    private readonly ILeaveAllocationService _leaveAllocationService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IAppDbContext context,
        IEmailService emailService,
        ILeaveAllocationService leaveAllocationService,
        ILogger<UserService> logger)
    {
        _context = context;
        _emailService = emailService;
        _leaveAllocationService = leaveAllocationService;
        _logger = logger;
    }

    private async Task<Guid?> GetOrganizationIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user?.OrganizationId;
    }

    public async Task<PagedResult<UserResponseDto>?> GetUsersAsync(
        Guid currentUserId,
        UserFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetOrganizationIdAsync(currentUserId, cancellationToken);
        if (orgId == null) return null;

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = $"%{filter.SearchTerm.Trim()}%";
            query = query.Where(u =>
                EF.Functions.Like(u.FullName, term) ||
                EF.Functions.Like(u.Email, term) ||
                (u.EmployeeCode != null && EF.Functions.Like(u.EmployeeCode, term)));
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(u => u.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Role) && Enum.TryParse<UserRole>(filter.Role, true, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

        int currentYear = DateTime.UtcNow.Year;
        var userIds = users.Select(u => u.Id).ToList();

        // Batch fetch balances to eliminate N+1 database calls
        var balancesGrouped = await _leaveAllocationService.GetUsersLeaveBalancesAsync(
            userIds, orgId.Value, currentYear, cancellationToken);

        foreach (var userDto in users)
        {
            if (balancesGrouped.TryGetValue(userDto.Id, out var balances))
            {
                userDto.LeaveBalances = balances;
            }
        }

        return new PagedResult<UserResponseDto>
        {
            Items = users,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<UserResponseDto>> GetDepartmentColleaguesAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (currentUser?.OrganizationId == null || currentUser.DepartmentId == null)
        {
            return Enumerable.Empty<UserResponseDto>();
        }

        return await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == currentUser.OrganizationId.Value &&
                        u.DepartmentId == currentUser.DepartmentId.Value &&
                        u.Id != currentUserId)
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

        var userDto = await _context.Users
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

        if (userDto != null)
        {
            int currentYear = DateTime.UtcNow.Year;
            var balances = await _leaveAllocationService.GetUserLeaveBalancesAsync(
                userDto.Id, orgId.Value, currentYear, cancellationToken);

            userDto.LeaveBalances = balances.ToList();
        }

        return userDto;
    }

    public async Task<ApiResponse<UserResponseDto>> ProvisionUserAsync(
        Guid hrUserId,
        ProvisionUserDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return ApiResponse<UserResponseDto>.FailureResponse("FullName and Email are required.", 400);
        }

        var hrUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == hrUserId, cancellationToken);
        if (hrUser?.OrganizationId == null)
        {
            return ApiResponse<UserResponseDto>.FailureResponse("HR account is not linked to any organization.", 400);
        }

        var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == hrUser.OrganizationId, cancellationToken);
        if (org == null)
        {
            return ApiResponse<UserResponseDto>.FailureResponse("Organization not found.", 400);
        }

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.OrganizationId == org.Id && u.Email.ToLower() == dto.Email.ToLower(), cancellationToken);

        if (emailExists)
        {
            return ApiResponse<UserResponseDto>.FailureResponse($"Email '{dto.Email}' already exists in your organization.", 400);
        }

        if (dto.DepartmentId.HasValue && !await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value && d.OrganizationId == org.Id, cancellationToken))
        {
            return ApiResponse<UserResponseDto>.FailureResponse("Selected department does not exist in your organization.", 400);
        }

        if (dto.TeamLeadId.HasValue && !await _context.Users.AnyAsync(u => u.Id == dto.TeamLeadId.Value && u.OrganizationId == org.Id, cancellationToken))
        {
            return ApiResponse<UserResponseDto>.FailureResponse("Selected team lead does not exist in your organization.", 400);
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

        var leaveTypes = await _context.LeaveTypes
            .Where(lt => lt.OrganizationId == org.Id)
            .ToListAsync(cancellationToken);

        int currentYear = DateTime.UtcNow.Year;
        foreach (var lt in leaveTypes)
        {
            await _context.LeaveAllocations.AddAsync(new LeaveAllocation
            {
                Id = Guid.NewGuid(),
                EmployeeId = newUser.Id,
                LeaveTypeId = lt.Id,
                NumberOfDays = lt.DefaultDays,
                Period = currentYear
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                string baseUrl = string.IsNullOrWhiteSpace(dto.ResetPasswordUrl)
                    ? "https://new-leave-management-system-qszg.vercel.app/reset-password"
                    : dto.ResetPasswordUrl.TrimEnd('/');

                string resetLink = $"{baseUrl}?token={resetToken}&email={Uri.EscapeDataString(newUser.Email)}";
                string emailBody = $"Welcome to LeaveFlow, {dto.FullName}! Use code {formattedCode} and temp password: {tempPassword}. Reset here: {resetLink}";

                await _emailService.SendEmailAsync(dto.Email, "Welcome to LeaveFlow", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", newUser.Email);
            }
        });

        var responseDto = new UserResponseDto
        {
            Id = newUser.Id,
            FullName = newUser.FullName,
            Email = newUser.Email,
            EmployeeCode = newUser.EmployeeCode,
            DepartmentId = newUser.DepartmentId,
            TeamLeadId = newUser.TeamLeadId,
            Designation = newUser.Designation,
            Role = newUser.Role.ToString(),
            LeaveBalance = newUser.LeaveBalance
        };

        return ApiResponse<UserResponseDto>.SuccessResponse(responseDto, "User provisioned successfully.", 201);
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserAsync(
        Guid id,
        Guid currentUserId,
        UpdateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetOrganizationIdAsync(currentUserId, cancellationToken);
        if (orgId == null) return ApiResponse<UserResponseDto>.FailureResponse("User organization not found.", 400);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.OrganizationId == orgId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserResponseDto>.FailureResponse($"User with ID '{id}' not found or does not belong to your organization.", 404);
        }

        if (dto.DepartmentId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value && d.OrganizationId == orgId, cancellationToken))
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Selected department does not exist in your organization.", 400);
            }
            user.DepartmentId = dto.DepartmentId;
        }

        if (dto.TeamLeadId.HasValue)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == dto.TeamLeadId.Value && u.OrganizationId == orgId, cancellationToken))
            {
                return ApiResponse<UserResponseDto>.FailureResponse("Selected team lead does not exist in your organization.", 400);
            }
            user.TeamLeadId = dto.TeamLeadId;
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Designation)) user.Designation = dto.Designation.Trim();
        if (dto.LeaveBalance.HasValue) user.LeaveBalance = Math.Max(0, dto.LeaveBalance.Value);
        if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole))
        {
            user.Role = parsedRole;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var responseDto = new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            EmployeeCode = user.EmployeeCode,
            DepartmentId = user.DepartmentId,
            TeamLeadId = user.TeamLeadId,
            Designation = user.Designation,
            Role = user.Role.ToString(),
            LeaveBalance = user.LeaveBalance
        };

        return ApiResponse<UserResponseDto>.SuccessResponse(responseDto, "Employee updated successfully.", 200);
    }

    public async Task<ApiResponse<BulkUploadResultDto>> BulkUploadUsersAsync(
        Guid hrUserId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            return ApiResponse<BulkUploadResultDto>.FailureResponse("Please upload a valid Excel file.", 400);

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<BulkUploadResultDto>.FailureResponse("Only .xlsx Excel files are supported.", 400);

        var hrUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == hrUserId, cancellationToken);
        if (hrUser?.OrganizationId == null)
            return ApiResponse<BulkUploadResultDto>.FailureResponse("HR account is not linked to any organization.", 400);

        var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == hrUser.OrganizationId, cancellationToken);
        if (org == null)
            return ApiResponse<BulkUploadResultDto>.FailureResponse("Organization not found.", 400);

        var departments = await _context.Departments.AsNoTracking().Where(d => d.OrganizationId == org.Id).ToListAsync(cancellationToken);
        var leaveTypes = await _context.LeaveTypes.AsNoTracking().Where(lt => lt.OrganizationId == org.Id).ToListAsync(cancellationToken);
        var existingEmails = await _context.Users.AsNoTracking().Where(u => u.OrganizationId == org.Id).Select(u => u.Email.ToLower()).ToListAsync(cancellationToken);

        var emailHashSet = new HashSet<string>(existingEmails);
        var createdUsers = new List<User>();
        var errors = new List<string>();
        int currentYear = DateTime.UtcNow.Year;
        int totalRows = 0;

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            return ApiResponse<BulkUploadResultDto>.FailureResponse("The uploaded Excel file contains no worksheets.", 400);

        var rows = worksheet.RowsUsed().Skip(1); // Skip header row

        foreach (var row in rows)
        {
            totalRows++;
            int rowNumber = row.RowNumber();

            string fullName = row.Cell(1).GetValue<string>()?.Trim() ?? string.Empty;
            string email = row.Cell(2).GetValue<string>()?.Trim() ?? string.Empty;
            string roleStr = row.Cell(3).GetValue<string>()?.Trim() ?? string.Empty;
            string designation = row.Cell(4).GetValue<string>()?.Trim() ?? string.Empty;
            string deptName = row.Cell(5).GetValue<string>()?.Trim() ?? string.Empty;

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

            Guid? deptId = string.IsNullOrWhiteSpace(deptName)
                ? null
                : departments.FirstOrDefault(d => d.Name.Equals(deptName, StringComparison.OrdinalIgnoreCase))?.Id;

            org.LastEmployeeNumber++;
            string formattedCode = $"{org.CodePrefix}-{org.LastEmployeeNumber:D2}";
            string tempPassword = "Welcome" + Random.Shared.Next(1000, 9999) + "!";

            if (!Enum.TryParse<UserRole>(roleStr, true, out var userRole))
            {
                userRole = UserRole.Employee;
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                Role = userRole,
                Designation = string.IsNullOrWhiteSpace(designation) ? "Employee" : designation,
                DepartmentId = deptId,
                OrganizationId = org.Id,
                EmployeeCode = formattedCode,
                LeaveBalance = 20,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(newUser, cancellationToken);
            createdUsers.Add(newUser);
            emailHashSet.Add(email.ToLower());

            foreach (var lt in leaveTypes)
            {
                await _context.LeaveAllocations.AddAsync(new LeaveAllocation
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = newUser.Id,
                    LeaveTypeId = lt.Id,
                    NumberOfDays = lt.DefaultDays,
                    Period = currentYear
                }, cancellationToken);
            }
        }

        if (createdUsers.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var resultData = new BulkUploadResultDto
        {
            Success = errors.Count == 0,
            Message = $"Bulk upload completed. {createdUsers.Count} employee(s) created.",
            TotalProcessed = totalRows,
            SuccessfullyCreated = createdUsers.Count,
            Errors = errors
        };

        return ApiResponse<BulkUploadResultDto>.SuccessResponse(resultData, resultData.Message, 200);
    }
}