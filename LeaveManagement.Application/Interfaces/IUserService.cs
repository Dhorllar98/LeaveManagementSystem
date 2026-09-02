using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.DTOs.User;
using LeaveManagement.Application.DTOs.Users;
using Microsoft.AspNetCore.Http;

namespace LeaveManagement.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserResponseDto>?> GetUsersAsync(
        Guid currentUserId,
        UserFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<UserResponseDto>> GetDepartmentColleaguesAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<UserResponseDto?> GetUserByIdAsync(
        Guid id,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int StatusCode, object? Data)> ProvisionUserAsync(
        Guid hrUserId,
        ProvisionUserDto dto,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int StatusCode, object? Data)> UpdateUserAsync(
        Guid id,
        Guid currentUserId,
        UpdateUserDto dto,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int StatusCode, BulkUploadResultDto? Data)> BulkUploadUsersAsync(
        Guid hrUserId,
        IFormFile file,
        CancellationToken cancellationToken = default);
}