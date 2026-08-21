using LeaveManagement.Application.DTOs.Settings;

namespace LeaveManagement.Application.Interfaces;

public interface ISettingsService
{
    Task<NotificationSettingsDto?> GetNotificationSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, NotificationSettingsDto? Data)> UpdateNotificationSettingsAsync(
        Guid userId,
        NotificationSettingsDto dto,
        CancellationToken cancellationToken = default);

    Task<OrganizationSettingsDto?> GetOrganizationSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, OrganizationSettingsDto? Data)> UpdateOrganizationSettingsAsync(
        Guid userId,
        UpdateOrganizationSettingsDto dto,
        CancellationToken cancellationToken = default);
}