using LeaveManagement.Application.DTOs.Profile;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : BaseController
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (user == null) return NotFound(new { message = "Profile not found." });

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.EmployeeCode,
            user.Designation,
            user.LeaveBalance,
            Role = user.Role.ToString(),
            DepartmentName = user.Department?.Name,
            user.OrganizationId
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        // Fetch the user making the request to get their OrganizationId and Role
        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (currentUser == null) return Unauthorized();

        // Determine which user we are trying to update
        var targetUserId = dto.UserId.HasValue && dto.UserId.Value != Guid.Empty
            ? dto.UserId.Value
            : currentUserId;

        // ROLE CHECK: Prevent standard employees from updating others (Only HR allowed)
        if (targetUserId != currentUserId && currentUser.Role != UserRole.HR)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to update other users' profiles." });
        }

        // 4. 🔒 STRICT TENANT FILTER: Ensure the target user belongs to the same organization
        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == targetUserId && u.OrganizationId == currentUser.OrganizationId, cancellationToken);

        if (targetUser == null)
        {
            return NotFound(new { message = "Target user not found or does not belong to your organization." });
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            targetUser.FullName = dto.FullName;

        if (dto.DepartmentId.HasValue)
            targetUser.DepartmentId = dto.DepartmentId.Value;

        if (!string.IsNullOrWhiteSpace(dto.Designation))
            targetUser.Designation = dto.Designation;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = targetUserId == currentUserId ? "Your profile was updated successfully." : "Employee profile updated successfully."
        });
    }
}