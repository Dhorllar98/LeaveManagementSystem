using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.DTOs.LeaveRequest;

namespace LeaveManagement.Application.Interfaces;

public interface ILeaveRequestService
{
    Task<(IEnumerable<LeaveRequestSummaryDto> Items, int TotalCount)?> GetPagedLeaveRequestsAsync(
        Guid userId,
        bool isLeadOrHr,
        LeaveRequestQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<LeaveRequestSummaryDto> Items, int TotalCount)?> GetTotalRequestsForHrAsync(
        Guid userId,
        LeaveRequestQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<LeaveRequestSummaryDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, object? Data, int StatusCode)> CreateLeaveRequestAsync(
        Guid userId,
        CreateLeaveRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int StatusCode, object? Data)> UpdateLeaveRequestAsync(
        Guid id,
        Guid userId,
        CreateLeaveRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int StatusCode)> DeleteLeaveRequestAsync(
        Guid id,
        Guid userId,
        bool isLeadOrHr,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int StatusCode)> ApproveLeaveAsync(
        Guid id,
        Guid currentUserId,
        ManagerActionDto dto,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int StatusCode)> RejectLeaveAsync(
        Guid id,
        Guid currentUserId,
        ManagerActionDto dto,
        CancellationToken cancellationToken = default);
}