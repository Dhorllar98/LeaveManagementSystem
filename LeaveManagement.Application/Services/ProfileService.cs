using LeaveManagement.Application.DTOs.Profile;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;

namespace LeaveManagement.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IAppDbContext _context;

    public ProfileService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileResponseDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return null;

        var formattedCode = !string.IsNullOrWhiteSpace(user.EmployeeCode) ? user.EmployeeCode : user.Id.ToString();
        var department = user.Department?.Name ?? "Human Resources";

        return new ProfileResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Department = department,
            DepartmentName = department,
            Designation = !string.IsNullOrWhiteSpace(user.Designation) ? user.Designation : user.Role.ToString(),
            EmployeeCode = formattedCode,
            EmployeeId = formattedCode,
            LeaveBalance = user.LeaveBalance,
            CreatedAt = user.CreatedAt,
            OrganizationId = user.OrganizationId
        };
    }

    public async Task<(bool Success, string Message, int StatusCode)> UpdateProfileAsync(
        Guid currentUserId,
        UpdateProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (currentUser == null) return (false, "Unauthorized", 401);

        var targetUserId = dto.UserId.HasValue && dto.UserId.Value != Guid.Empty
            ? dto.UserId.Value
            : currentUserId;

        if (targetUserId != currentUserId && currentUser.Role != UserRole.HR)
        {
            return (false, "You do not have permission to update other users' profiles.", 403);
        }

        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == targetUserId && u.OrganizationId == currentUser.OrganizationId, cancellationToken);

        if (targetUser == null)
        {
            return (false, "Target user not found or does not belong to your organization.", 404);
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            targetUser.FullName = dto.FullName;

        if (dto.DepartmentId.HasValue)
            targetUser.DepartmentId = dto.DepartmentId.Value;

        if (!string.IsNullOrWhiteSpace(dto.Designation))
            targetUser.Designation = dto.Designation;

        await _context.SaveChangesAsync(cancellationToken);

        string message = targetUserId == currentUserId
            ? "Your profile was updated successfully."
            : "Employee profile updated successfully.";

        return (true, message, 200);
    }
}