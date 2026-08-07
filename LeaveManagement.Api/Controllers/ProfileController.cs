using LeaveManagement.Application.DTOs.Profile;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[Authorize]
public class ProfileController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProfileController(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        var nameParts = (user.FullName ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        return Ok(new
        {
            success = true,
            data = new
            {
                id = user.Id,
                name = new
                {
                    fullName = user.FullName,
                    firstName = firstName,
                    lastName = lastName
                },
                email = user.Email,
                role = user.Role.ToString(),
                department = user.Department,
                designation = user.Designation,
                leaveBalance = user.LeaveBalance,
                createdAt = user.CreatedAt
            }
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        user.FullName = string.IsNullOrWhiteSpace(dto.FullName) ? user.FullName : dto.FullName;
        user.Department = dto.Department;
        user.Designation = dto.Designation;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var nameParts = (user.FullName ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        return Ok(new
        {
            success = true,
            message = "Profile updated successfully.",
            data = new
            {
                id = user.Id,
                name = new
                {
                    fullName = user.FullName,
                    firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty,
                    lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty
                },
                email = user.Email,
                role = user.Role.ToString(),
                department = user.Department,
                designation = user.Designation,
                leaveBalance = user.LeaveBalance,
                createdAt = user.CreatedAt
            }
        });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Incorrect current password." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Password updated successfully." });
    }
}