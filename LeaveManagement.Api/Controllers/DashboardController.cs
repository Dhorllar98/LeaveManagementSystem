using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.DTOs.Dashboard;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : BaseController
{
    private readonly ILeaveRequestRepository _leaveRepository;
    private readonly IUserRepository _userRepository;

    public DashboardController(ILeaveRequestRepository leaveRepository, IUserRepository userRepository)
    {
        _leaveRepository = leaveRepository;
        _userRepository = userRepository;
    }

    // Accessible by all authenticated roles (Employee, TeamLead, HR)
    [HttpGet("user-stats")]
    public async Task<IActionResult> GetUserDashboardStats(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return NotFound(ApiResponse<string>.FailureResponse("User not found."));

        // Uses your existing repository method: GetByEmployeeIdAsync
        var userRequests = await _leaveRepository.GetByEmployeeIdAsync(userId, cancellationToken)
                           ?? new List<Domain.Entities.LeaveRequest>();

        var stats = new DashboardResponseDto
        {
            EmployeeName = user.FullName,
            PendingRequestsCount = userRequests.Count(r => r.Status == LeaveStatus.Pending),
            ApprovedLeavesCount = userRequests.Count(r => r.Status == LeaveStatus.Approved),
            RejectedLeavesCount = userRequests.Count(r => r.Status == LeaveStatus.Rejected),
            TotalLeaveDaysRemaining = user.LeaveBalance
        };

        return Ok(ApiResponse<DashboardResponseDto>.SuccessResponse(stats, "User stats retrieved successfully."));
    }

    // Accessible strictly by HR role
    [HttpGet("admin-stats")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetAdminDashboardStats(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        var adminUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (adminUser?.OrganizationId == null)
        {
            return BadRequest(ApiResponse<string>.FailureResponse("User organization not found."));
        }

        var orgId = adminUser.OrganizationId.Value;

        var orgLeaves = (await _leaveRepository.GetAllByOrganizationAsync(orgId, cancellationToken))?.ToList()
                        ?? new List<Domain.Entities.LeaveRequest>();
        var orgUsers = (await _userRepository.GetAllByOrganizationAsync(orgId, cancellationToken))?.ToList()
                       ?? new List<Domain.Entities.User>();

        var stats = new AdminDashboardResponseDto
        {
            TotalEmployees = orgUsers.Count,
            TotalRequestsCount = orgLeaves.Count,
            PendingApprovalsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Pending),
            ApprovedRequestsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Approved),
            RejectedRequestsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Rejected),
            EmployeesCurrentlyOnLeave = orgLeaves.Count(l => l.Status == LeaveStatus.Approved
                                                            && l.StartDate <= DateTime.UtcNow
                                                            && l.EndDate >= DateTime.UtcNow)
        };

        return Ok(ApiResponse<AdminDashboardResponseDto>.SuccessResponse(stats, "Admin stats retrieved successfully."));
    }
}