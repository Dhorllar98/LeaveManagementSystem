using LeaveManagement.Application.Common.Models;
using LeaveManagement.Application.DTOs.Dashboard;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : BaseController
{
    private readonly ILeaveRequestRepository _leaveRepository;
    private readonly IUserRepository _userRepository;
    private readonly AppDbContext _context;

    public DashboardController(
        ILeaveRequestRepository leaveRepository,
        IUserRepository userRepository,
        AppDbContext context)
    {
        _leaveRepository = leaveRepository;
        _userRepository = userRepository;
        _context = context;
    }

    [HttpGet("user-stats")]
    public async Task<IActionResult> GetUserDashboardStats(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return NotFound(ApiResponse<string>.FailureResponse("User not found."));

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

    [HttpGet("admin-stats")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetAdminDashboardStats(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        var adminUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (adminUser?.OrganizationId == null)
        {
            return BadRequest(ApiResponse<string>.FailureResponse("User organization not found."));
        }

        var orgId = adminUser.OrganizationId.Value;

        var orgLeaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.OrganizationId == orgId)
            .ToListAsync(cancellationToken);

        var orgUsers = await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == orgId)
            .ToListAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;

        var stats = new AdminDashboardResponseDto
        {
            TotalEmployees = orgUsers.Count,
            TotalRequestsCount = orgLeaves.Count,
            PendingApprovalsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Pending),
            ApprovedRequestsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Approved),
            RejectedRequestsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Rejected),
            EmployeesCurrentlyOnLeave = orgLeaves.Count(l => l.Status == LeaveStatus.Approved
                                                             && l.StartDate.Date <= today
                                                             && l.EndDate.Date >= today)
        };

        return Ok(ApiResponse<AdminDashboardResponseDto>.SuccessResponse(stats, "Admin stats retrieved successfully."));
    }
}