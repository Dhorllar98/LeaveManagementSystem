using LeaveManagement.Application.DTOs.Settings;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace LeaveManagement.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IAppDbContext _context;

    public SettingsService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationSettingsDto?> GetNotificationSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user?.OrganizationId == null) return null;

        var settings = await _context.NotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganizationId == user.OrganizationId.Value, cancellationToken);

        return new NotificationSettingsDto
        {
            EnableNewLeaveRequestEmails = settings?.EnableNewLeaveRequestEmails ?? true,
            EnableLeaveStatusUpdateEmails = settings?.EnableLeaveStatusUpdateEmails ?? true
        };
    }

    public async Task<(bool Success, string Message, NotificationSettingsDto? Data)> UpdateNotificationSettingsAsync(
        Guid userId,
        NotificationSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user?.OrganizationId == null)
        {
            return (false, "User organization not found.", null);
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

        return (true, "Notification settings updated successfully.", dto);
    }
}