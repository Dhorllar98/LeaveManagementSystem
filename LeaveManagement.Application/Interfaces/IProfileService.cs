using LeaveManagement.Application.DTOs.Profile;

namespace LeaveManagement.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileResponseDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, int StatusCode)> UpdateProfileAsync(Guid currentUserId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
}