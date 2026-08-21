using LeaveManagement.Application.DTOs.Settings;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace LeaveManagement.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IAppDbContext _context;
    private readonly IPhotoService _photoService;

    public SettingsService(IAppDbContext context, IPhotoService photoService)
    {
        _context = context;
        _photoService = photoService;
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

    public async Task<OrganizationSettingsDto?> GetOrganizationSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user?.OrganizationId == null) return null;

        var org = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == user.OrganizationId.Value, cancellationToken);

        if (org == null) return null;

        return new OrganizationSettingsDto
        {
            Id = org.Id,
            CompanyName = org.Name,
            CodePrefix = org.CodePrefix,
            LogoUrl = org.LogoUrl,
            Industry = org.Industry,
            CompanySize = org.CompanySize,
            Website = org.Website
        };
    }

    public async Task<(bool Success, string Message, OrganizationSettingsDto? Data)> UpdateOrganizationSettingsAsync(
        Guid userId,
        UpdateOrganizationSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user?.OrganizationId == null)
        {
            return (false, "User organization not found.", null);
        }

        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == user.OrganizationId.Value, cancellationToken);

        if (org == null)
        {
            return (false, "Organization record not found.", null);
        }

        org.Name = dto.CompanyName;
        org.Industry = dto.Industry;
        org.CompanySize = dto.CompanySize;
        org.Website = dto.Website;
        org.UpdatedAt = DateTime.UtcNow;

        if (dto.CompanyLogo != null && dto.CompanyLogo.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".svg", ".webp" };
            var extension = Path.GetExtension(dto.CompanyLogo.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return (false, "Invalid file format. Logo must be JPG, PNG, WEBP, or SVG.", null);
            }

            if (dto.CompanyLogo.Length > 2 * 1024 * 1024)
            {
                return (false, "Logo file size cannot exceed 2 MB.", null);
            }

            org.LogoUrl = await _photoService.UploadImageAsync(dto.CompanyLogo, cancellationToken);
        }

        _context.Organizations.Update(org);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new OrganizationSettingsDto
        {
            Id = org.Id,
            CompanyName = org.Name,
            CodePrefix = org.CodePrefix,
            LogoUrl = org.LogoUrl,
            Industry = org.Industry,
            CompanySize = org.CompanySize,
            Website = org.Website
        };

        return (true, "Organization settings updated successfully.", resultDto);
    }
}