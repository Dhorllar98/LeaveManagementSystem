using LeaveManagement.Application.DTOs.User;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var users = await _userService.GetUsersAsync(currentUserId, cancellationToken);

        if (users == null)
        {
            return BadRequest(new { message = "User organization not found." });
        }

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(id, currentUserId, cancellationToken);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{id}' not found or does not belong to your organization." });
        }

        return Ok(user);
    }

    [HttpPost("provision")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> ProvisionUser([FromBody] ProvisionUserDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "User identity invalid." });
        }

        var (success, message, statusCode, data) = await _userService.ProvisionUserAsync(currentUserId, dto, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                401 => Unauthorized(new { message }),
                404 => NotFound(new { message }),
                _ => BadRequest(new { message })
            };
        }

        return StatusCode(statusCode, new
        {
            success = true,
            message,
            data
        });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var (success, message, statusCode, data) = await _userService.UpdateUserAsync(id, currentUserId, dto, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                404 => NotFound(new { message }),
                _ => BadRequest(new { message })
            };
        }

        return StatusCode(statusCode, new
        {
            success = true,
            message,
            data
        });
    }

    [HttpPost("bulk-upload")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> BulkUploadUsers(IFormFile file, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "User identity invalid." });
        }

        (bool success, string message, int statusCode, BulkUploadResultDto? data) =
            await _userService.BulkUploadUsersAsync(currentUserId, file, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                401 => Unauthorized(new { message }),
                _ => BadRequest(new { message })
            };
        }

        return StatusCode(statusCode, data);
    }
}