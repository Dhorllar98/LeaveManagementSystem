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
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _userService.GetUsersAsync(currentUserId, filter, cancellationToken);

        if (result == null)
        {
            return BadRequest(new { message = "User organization not found." });
        }

        return Ok(result);
    }

    [HttpGet("department-colleagues")]
    public async Task<IActionResult> GetDepartmentColleagues(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _userService.GetDepartmentColleaguesAsync(currentUserId, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "Department colleagues retrieved successfully.",
            data = result
        });
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

        var result = await _userService.ProvisionUserAsync(currentUserId, dto, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _userService.UpdateUserAsync(id, currentUserId, dto, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result);
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

        var result = await _userService.BulkUploadUsersAsync(currentUserId, file, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result.Data);
    }
}