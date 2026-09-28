using System.Data;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILeaveAllocationService _leaveAllocationService;
    private readonly ILogger<UserService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public UserService(
        IAppDbContext context,
        IEmailService emailService,
        ILeaveAllocationService leaveAllocationService,
        ILogger<UserService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _leaveAllocationService = leaveAllocationService;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    private string GetResetPasswordBaseUrl(string? dtoUrl)
    {
        if (!string.IsNullOrWhiteSpace(dtoUrl))
        {
            return dtoUrl.TrimEnd('/');
        }

        var configUrl = _configuration["AppSettings:FrontendResetPasswordUrl"];
        return !string.IsNullOrWhiteSpace(configUrl)
            ? configUrl.TrimEnd('/')
            : "https://new-leave-management-system-qszg.vercel.app/reset-password";
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
                OrganizationId = u.OrganizationId ?? Guid.Empty,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                TeamLeadId = u.TeamLeadId,
                TeamLeadName = u.TeamLead != null ? u.TeamLead.FullName : null,
                Designation = u.Designation,
                Role = u.Role.ToString(),
                LeaveBalance = u.LeaveBalance,
                DateOfBirth = u.DateOfBirth.HasValue ? DateOnly.FromDateTime(u.DateOfBirth.Value) : null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        int currentYear = DateTime.UtcNow.Year;
        var userIds = users.Select(u => u.Id).ToList();

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
            .Where(u => u.OrganizationId == currentUser.OrganizationId &&
                        u.DepartmentId == currentUser.DepartmentId &&
                        u.Id != currentUserId)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                EmployeeCode = u.EmployeeCode,
                OrganizationId = u.OrganizationId ?? Guid.Empty,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                TeamLeadId = u.TeamLeadId,
                TeamLeadName = u.TeamLead != null ? u.TeamLead.FullName : null,
                Designation = u.Designation,
                Role = u.Role.ToString(),
                LeaveBalance = u.LeaveBalance,
                DateOfBirth = u.DateOfBirth.HasValue ? DateOnly.FromDateTime(u.DateOfBirth.Value) : null,
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
                OrganizationId = u.OrganizationId ?? Guid.Empty,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                TeamLeadId = u.TeamLeadId,
                TeamLeadName = u.TeamLead != null ? u.TeamLead.FullName : null,
                Designation = u.Designation,
                Role = u.Role.ToString(),
                LeaveBalance = u.LeaveBalance,
                DateOfBirth = u.DateOfBirth.HasValue ? DateOnly.FromDateTime(u.DateOfBirth.Value) : null,
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

        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.OrganizationId == hrUser.OrganizationId && u.Email.ToLower() == dto.Email.ToLower(), cancellationToken);

        if (emailExists)
        {
            return ApiResponse<UserResponseDto>.FailureResponse($"Email '{dto.Email}' already exists in your organization.", 400);
        }

        if (dto.DepartmentId.HasValue && !await _context.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value && d.OrganizationId == hrUser.OrganizationId, cancellationToken))
        {
            return ApiResponse<UserResponseDto>.FailureResponse("Selected department does not exist in your organization.", 400);
        }

        if (dto.TeamLeadId.HasValue && !await _context.Users.AnyAsync(u => u.Id == dto.TeamLeadId.Value && u.OrganizationId == hrUser.OrganizationId, cancellationToken))
        {
            return ApiResponse<UserResponseDto>.FailureResponse("Selected team lead does not exist in your organization.", 400);
        }

        if (!string.IsNullOrWhiteSpace(dto.Role) && !Enum.TryParse<UserRole>(dto.Role, true, out _))
        {
            return ApiResponse<UserResponseDto>.FailureResponse($"Invalid role '{dto.Role}' specified.", 400);
        }

        User newUser;
        string tempPassword;
        string resetToken;

        var strategy = _context.Database.CreateExecutionStrategy();

        var result = await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var org = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == hrUser.OrganizationId, cancellationToken);

            if (org == null)
            {
                throw new InvalidOperationException("Organization not found.");
            }

            org.LastEmployeeNumber++;
            string formattedCode = string.IsNullOrWhiteSpace(dto.EmployeeCode)
                ? $"{org.CodePrefix}-{org.LastEmployeeNumber:D2}"
                : dto.EmployeeCode.Trim();

            if (!string.IsNullOrWhiteSpace(dto.EmployeeCode))
            {
                bool codeExists = await _context.Users
                    .AnyAsync(u => u.OrganizationId == org.Id && u.EmployeeCode == formattedCode, cancellationToken);

                if (codeExists)
                {
                    throw new InvalidOperationException($"Employee code '{formattedCode}' is already in use.");
                }
            }

            Enum.TryParse<UserRole>(dto.Role, true, out var userRole);

            tempPassword = "Welcome" + Random.Shared.Next(1000, 9999) + "!";
            resetToken = Guid.NewGuid().ToString("N");

            newUser = new User
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
                DateOfBirth = dto.DateOfBirth.HasValue ? dto.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : null,
                PasswordResetToken = resetToken,
                ResetTokenExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(newUser, cancellationToken);

            var leaveTypes = await _context.LeaveTypes
                .Where(lt => lt.OrganizationId == org.Id)
                .ToListAsync(cancellationToken);

            int currentYear = DateTime.UtcNow.Year;
            var allocations = leaveTypes.Select(lt => new LeaveAllocation
            {
                Id = Guid.NewGuid(),
                EmployeeId = newUser.Id,
                LeaveTypeId = lt.Id,
                NumberOfDays = lt.DefaultDays,
                Period = currentYear
            });

            await _context.LeaveAllocations.AddRangeAsync(allocations, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (newUser, tempPassword, resetToken);
        });

        newUser = result.newUser;
        tempPassword = result.tempPassword;
        resetToken = result.resetToken;

        string baseUrl = GetResetPasswordBaseUrl(dto.ResetPasswordUrl);
        string resetLink = $"{baseUrl}?token={resetToken}&email={Uri.EscapeDataString(newUser.Email)}";
        string emailBody = $"Welcome to LeaveFlow, {dto.FullName}! Use code {newUser.EmployeeCode} and temp password: {tempPassword}. Reset here: {resetLink}";
        string recipientEmail = dto.Email.Trim();

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserService>>();

            try
            {
                await emailService.SendEmailAsync(recipientEmail, "Welcome to LeaveFlow", emailBody);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send welcome email to {Email}", recipientEmail);
            }
        });

        var responseDto = new UserResponseDto
        {
            Id = newUser.Id,
            OrganizationId = newUser.OrganizationId ?? Guid.Empty,
            FullName = newUser.FullName,
            Email = newUser.Email,
            EmployeeCode = newUser.EmployeeCode,
            DepartmentId = newUser.DepartmentId,
            TeamLeadId = newUser.TeamLeadId,
            Designation = newUser.Designation,
            Role = newUser.Role.ToString(),
            LeaveBalance = newUser.LeaveBalance,
            DateOfBirth = newUser.DateOfBirth.HasValue ? DateOnly.FromDateTime(newUser.DateOfBirth.Value) : null,
            CreatedAt = newUser.CreatedAt
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

        if (!string.IsNullOrWhiteSpace(dto.EmployeeCode) && dto.EmployeeCode != user.EmployeeCode)
        {
            bool codeExists = await _context.Users.AnyAsync(u => u.OrganizationId == orgId && u.EmployeeCode == dto.EmployeeCode.Trim() && u.Id != id, cancellationToken);
            if (codeExists)
            {
                return ApiResponse<UserResponseDto>.FailureResponse($"Employee code '{dto.EmployeeCode}' is already assigned to another user.", 400);
            }
            user.EmployeeCode = dto.EmployeeCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            if (Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole))
            {
                user.Role = parsedRole;
            }
            else
            {
                return ApiResponse<UserResponseDto>.FailureResponse($"Invalid role '{dto.Role}' specified.", 400);
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Designation)) user.Designation = dto.Designation.Trim();
        if (dto.LeaveBalance.HasValue) user.LeaveBalance = Math.Max(0, dto.LeaveBalance.Value);
        if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);

        await _context.SaveChangesAsync(cancellationToken);

        var responseDto = new UserResponseDto
        {
            Id = user.Id,
            OrganizationId = user.OrganizationId ?? Guid.Empty,
            FullName = user.FullName,
            Email = user.Email,
            EmployeeCode = user.EmployeeCode,
            DepartmentId = user.DepartmentId,
            TeamLeadId = user.TeamLeadId,
            Designation = user.Designation,
            Role = user.Role.ToString(),
            LeaveBalance = user.LeaveBalance,
            DateOfBirth = user.DateOfBirth.HasValue ? DateOnly.FromDateTime(user.DateOfBirth.Value) : null,
            CreatedAt = user.CreatedAt
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

        var orgId = hrUser.OrganizationId.Value;

        var departments = await _context.Departments.AsNoTracking().Where(d => d.OrganizationId == orgId).ToListAsync(cancellationToken);
        var leaveTypes = await _context.LeaveTypes.AsNoTracking().Where(lt => lt.OrganizationId == orgId).ToListAsync(cancellationToken);
        var existingEmails = await _context.Users.AsNoTracking().Where(u => u.OrganizationId == orgId).Select(u => u.Email.ToLower()).ToListAsync(cancellationToken);

        var emailHashSet = new HashSet<string>(existingEmails);
        var errors = new List<string>();
        int totalRows = 0;

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            return ApiResponse<BulkUploadResultDto>.FailureResponse("The uploaded Excel file contains no worksheets.", 400);

        var rows = worksheet.RowsUsed().Skip(1);
        var parsedItems = new List<(string FullName, string Email, UserRole Role, string Designation, Guid? DeptId, DateOnly? Dob)>();

        foreach (var row in rows)
        {
            totalRows++;
            int rowNumber = row.RowNumber();

            string fullName = row.Cell(1).GetValue<string>()?.Trim() ?? string.Empty;
            string email = row.Cell(2).GetValue<string>()?.Trim() ?? string.Empty;
            string roleStr = row.Cell(3).GetValue<string>()?.Trim() ?? string.Empty;
            string designation = row.Cell(4).GetValue<string>()?.Trim() ?? string.Empty;
            string deptName = row.Cell(5).GetValue<string>()?.Trim() ?? string.Empty;

            DateOnly? dob = null;
            var dobCell = row.Cell(6);
            if (!dobCell.IsEmpty())
            {
                if (dobCell.DataType == XLDataType.DateTime)
                {
                    dob = DateOnly.FromDateTime(dobCell.GetDateTime());
                }
                else if (DateOnly.TryParse(dobCell.GetValue<string>(), CultureInfo.InvariantCulture, out var parsedDob))
                {
                    dob = parsedDob;
                }
            }

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

            if (!Enum.TryParse<UserRole>(roleStr, true, out var userRole))
            {
                userRole = UserRole.Employee;
            }

            emailHashSet.Add(email.ToLower());
            parsedItems.Add((fullName, email, userRole, string.IsNullOrWhiteSpace(designation) ? "Employee" : designation, deptId, dob));
        }

        if (parsedItems.Count == 0)
        {
            return ApiResponse<BulkUploadResultDto>.SuccessResponse(new BulkUploadResultDto
            {
                Success = false,
                Message = "No valid records were found to process.",
                TotalProcessed = totalRows,
                SuccessfullyCreated = 0,
                Errors = errors
            }, "Bulk upload completed with zero insertions.", 200);
        }

        var preparedUsers = new System.Collections.Concurrent.ConcurrentBag<(User User, string TempPassword, string ResetToken)>();

        // Parallelizing CPU-intensive BCrypt Hashing
        Parallel.ForEach(parsedItems, item =>
        {
            string tempPassword = "Welcome" + Random.Shared.Next(1000, 9999) + "!";
            string resetToken = Guid.NewGuid().ToString("N");
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = item.FullName,
                Email = item.Email,
                PasswordHash = passwordHash,
                Role = item.Role,
                Designation = item.Designation,
                DepartmentId = item.DeptId,
                OrganizationId = orgId,
                LeaveBalance = 20,
                DateOfBirth = item.Dob.HasValue ? item.Dob.Value.ToDateTime(TimeOnly.MinValue) : null,
                PasswordResetToken = resetToken,
                ResetTokenExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            preparedUsers.Add((user, tempPassword, resetToken));
        });

        var createdUsersList = preparedUsers.ToList();
        int currentYear = DateTime.UtcNow.Year;

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, cancellationToken);
            if (org == null) throw new InvalidOperationException("Organization not found.");

            var newUsers = new List<User>();
            var newAllocations = new List<LeaveAllocation>();

            foreach (var item in createdUsersList)
            {
                org.LastEmployeeNumber++;
                item.User.EmployeeCode = $"{org.CodePrefix}-{org.LastEmployeeNumber:D2}";
                newUsers.Add(item.User);

                foreach (var lt in leaveTypes)
                {
                    newAllocations.Add(new LeaveAllocation
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = item.User.Id,
                        LeaveTypeId = lt.Id,
                        NumberOfDays = lt.DefaultDays,
                        Period = currentYear
                    });
                }
            }

            await _context.Users.AddRangeAsync(newUsers, cancellationToken);
            await _context.LeaveAllocations.AddRangeAsync(newAllocations, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        string baseUrl = GetResetPasswordBaseUrl(null);
        var usersToSend = createdUsersList.ToList();

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserService>>();

            foreach (var item in usersToSend)
            {
                try
                {
                    string resetLink = $"{baseUrl}?token={item.ResetToken}&email={Uri.EscapeDataString(item.User.Email)}";
                    string emailBody = $"Welcome to LeaveFlow, {item.User.FullName}! Use code {item.User.EmployeeCode} and temp password: {item.TempPassword}. Reset here: {resetLink}";

                    await emailService.SendEmailAsync(item.User.Email, "Welcome to LeaveFlow", emailBody);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send bulk welcome email to {Email}", item.User.Email);
                }
            }
        });

        var resultData = new BulkUploadResultDto
        {
            Success = errors.Count == 0,
            Message = $"Bulk upload completed. {createdUsersList.Count} employee(s) created.",
            TotalProcessed = totalRows,
            SuccessfullyCreated = createdUsersList.Count,
            Errors = errors
        };

        return ApiResponse<BulkUploadResultDto>.SuccessResponse(resultData, resultData.Message, 200);
    }
}