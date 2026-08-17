using FluentValidation;
using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.DTOs.Auth;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Exceptions;
using LeaveManagement.Domain.Interfaces;

namespace LeaveManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;

    public AuthService(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IDepartmentRepository departmentRepository,
        ILeaveTypeRepository leaveTypeRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator)
    {
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;
        _departmentRepository = departmentRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterOrganizationAsync(RegisterOrganizationDto request, CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = request.CodePrefix.Trim().ToUpper();

        if (await _organizationRepository.ExistsByPrefixAsync(normalizedPrefix, cancellationToken))
        {
            throw new ConflictException($"Organization with prefix '{normalizedPrefix}' already exists.");
        }

        if (await _userRepository.EmailExistsAsync(request.AdminEmail, cancellationToken))
        {
            throw new ConflictException($"User with email '{request.AdminEmail}' already exists.");
        }

        // Create Organization
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName,
            CodePrefix = normalizedPrefix,
            LastEmployeeNumber = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Create Default HR Department
        var hrDepartment = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Human Resources",
            OrganizationId = organization.Id,
            CreatedAt = DateTime.UtcNow
        };

        // Create Default Leave Types with OrganizationId bound
        var defaultLeaveTypes = new List<LeaveType>
        {
            new() { Id = Guid.NewGuid(), Name = "Annual Leave", DefaultDays = 20, OrganizationId = organization.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Sick Leave", DefaultDays = 10, OrganizationId = organization.Id, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Maternity/Paternity Leave", DefaultDays = 30, OrganizationId = organization.Id, CreatedAt = DateTime.UtcNow }
        };

        // Create Initial HR Admin Account
        var hrAdmin = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.AdminFullName,
            Email = request.AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.HR,
            Designation = "HR Manager",
            OrganizationId = organization.Id,
            DepartmentId = hrDepartment.Id,
            EmployeeCode = $"{normalizedPrefix}-01",
            LeaveBalance = 20,
            CreatedAt = DateTime.UtcNow
        };

        // Generate Auth Tokens
        var token = _jwtTokenGenerator.GenerateAccessToken(hrAdmin);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        hrAdmin.RefreshToken = refreshToken;
        hrAdmin.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        // Save via Repositories & UnitOfWork
        await _organizationRepository.AddAsync(organization, cancellationToken);
        await _departmentRepository.AddAsync(hrDepartment);
        foreach (var leaveType in defaultLeaveTypes)
        {
            await _leaveTypeRepository.AddAsync(leaveType);
        }
        await _userRepository.AddAsync(hrAdmin, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(new AuthResponseDto
        {
            UserId = hrAdmin.Id,
            FullName = hrAdmin.FullName,
            Email = hrAdmin.Email,
            Department = hrDepartment.Name,
            Designation = hrAdmin.Designation,
            Role = hrAdmin.Role,
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(30)
        }, "Organization registered successfully.");
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictException($"User with email {request.Email} already exists.");
        }

        // Fetch department to inherit tenant OrganizationId
        var department = request.DepartmentId.HasValue
            ? await _departmentRepository.GetByIdAsync(request.DepartmentId.Value)
            : null;

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            DepartmentId = request.DepartmentId,
            OrganizationId = department?.OrganizationId, // Automatically binds employee to organization
            Designation = request.Designation,
            LeaveBalance = 20,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdUser = await _userRepository.GetByIdAsync(user.Id, cancellationToken) ?? user;

        var token = _jwtTokenGenerator.GenerateAccessToken(createdUser);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        createdUser.RefreshToken = refreshToken;
        createdUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _userRepository.Update(createdUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(new AuthResponseDto
        {
            UserId = createdUser.Id,
            FullName = createdUser.FullName,
            Email = createdUser.Email,
            Department = createdUser.Department?.Name ?? string.Empty,
            Designation = createdUser.Designation,
            Role = createdUser.Role,
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(30)
        }, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Domain.Exceptions.ValidationException("Invalid login request payload.");
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Department = user.Department?.Name ?? string.Empty,
            Designation = user.Designation,
            Role = user.Role,
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(30)
        }, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new Domain.Exceptions.ValidationException("Refresh token is required.");

        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<AuthResponseDto>.SuccessResponse(new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Department = user.Department?.Name ?? string.Empty,
            Designation = user.Designation,
            Role = user.Role,
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(30)
        }, "Token refreshed successfully.");
    }
}