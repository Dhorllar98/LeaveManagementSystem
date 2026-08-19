using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.DTOs.Dashboard;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("user-stats")]
    public async Task<IActionResult> GetUserDashboardStats(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var stats = await _dashboardService.GetUserDashboardStatsAsync(userId, cancellationToken);
        if (stats == null)
            return NotFound(ApiResponse<string>.FailureResponse("User not found."));

        return Ok(ApiResponse<DashboardResponseDto>.SuccessResponse(stats, "User stats retrieved successfully."));
    }

    [HttpGet("admin-stats")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetAdminDashboardStats(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        var stats = await _dashboardService.GetAdminDashboardStatsAsync(currentUserId, cancellationToken);
        if (stats == null)
            return BadRequest(ApiResponse<string>.FailureResponse("User organization not found."));

        return Ok(ApiResponse<AdminDashboardResponseDto>.SuccessResponse(stats, "Admin stats retrieved successfully."));
    }
}