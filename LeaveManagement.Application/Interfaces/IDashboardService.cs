using LeaveManagement.Application.DTOs.Dashboard;

namespace LeaveManagement.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto?> GetUserDashboardStatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AdminDashboardResponseDto?> GetAdminDashboardStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}