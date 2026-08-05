using System.Security.Claims;
using LeaveManagement.Application.DTOs.Profile;
using LeaveManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[Authorize]
public class UsersController : BaseController
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Department,
                u.Designation,
                Role = u.Role.ToString(),
                u.LeaveBalance
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            return NotFound("User not found.");

        var profile = new ProfileResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Department = user.Department,
            Designation = user.Designation,
            LeaveBalance = user.LeaveBalance,
            CreatedAt = user.CreatedAt
        };

        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
            return NotFound("User not found.");

        user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? user.FullName : request.FullName;
        user.Department = request.Department;
        user.Designation = request.Designation;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var response = new ProfileResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Department = user.Department,
            Designation = user.Designation,
            LeaveBalance = user.LeaveBalance,
            CreatedAt = user.CreatedAt
        };

        return Ok(response);
    }
}