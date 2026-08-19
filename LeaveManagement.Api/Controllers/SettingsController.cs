using LeaveManagement.Application.DTOs.Settings;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : BaseController
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Fetch current notification settings for the HR's organization.
    /// </summary>
    [HttpGet("notifications")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetNotificationSettings(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var settings = await _settingsService.GetNotificationSettingsAsync(currentUserId, cancellationToken);

        if (settings == null)
        {
            return BadRequest(new { success = false, message = "User organization not found." });
        }

        return Ok(new { success = true, data = settings });
    }

    /// <summary>
    /// Update notification settings for the HR's organization.
    /// </summary>
    [HttpPut("notifications")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> UpdateNotificationSettings(
        [FromBody] NotificationSettingsDto dto,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var (success, message, data) = await _settingsService.UpdateNotificationSettingsAsync(currentUserId, dto, cancellationToken);

        if (!success)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new
        {
            success = true,
            message,
            data
        });
    }
}