using LeaveManagement.Application.DTOs.Settings;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : BaseController
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Fetch current notification settings for the HR's organization.
    /// </summary>
    [HttpGet("notifications")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetNotificationSettings(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (user?.OrganizationId == null)
        {
            return BadRequest(new { success = false, message = "User organization not found." });
        }

        var settings = await _context.NotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == user.OrganizationId.Value, cancellationToken);

        var response = new NotificationSettingsDto
        {
            EnableNewLeaveRequestEmails = settings?.EnableNewLeaveRequestEmails ?? true,
            EnableLeaveStatusUpdateEmails = settings?.EnableLeaveStatusUpdateEmails ?? true
        };

        return Ok(new { success = true, data = response });
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
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (user?.OrganizationId == null)
        {
            return BadRequest(new { success = false, message = "User organization not found." });
        }

        var settings = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == user.OrganizationId.Value, cancellationToken);

        if (settings == null)
        {
            settings = new NotificationSetting
            {
                Id = Guid.NewGuid(),
                OrganizationId = user.OrganizationId.Value,
                EnableNewLeaveRequestEmails = dto.EnableNewLeaveRequestEmails,
                EnableLeaveStatusUpdateEmails = dto.EnableLeaveStatusUpdateEmails,
                CreatedAt = DateTime.UtcNow
            };

            await _context.NotificationSettings.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.EnableNewLeaveRequestEmails = dto.EnableNewLeaveRequestEmails;
            settings.EnableLeaveStatusUpdateEmails = dto.EnableLeaveStatusUpdateEmails;
            settings.UpdatedAt = DateTime.UtcNow;

            _context.NotificationSettings.Update(settings);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Notification settings updated successfully.",
            data = dto
        });
    }
}