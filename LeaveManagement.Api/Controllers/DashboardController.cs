using LeaveManagement.Application.DTOs.Dashboard;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

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

    // GET /api/Dashboard/user-stats (Employee Dashboard)
    [HttpGet("user-stats")]
    public async Task<ActionResult<DashboardResponseDto>> GetUserDashboardStats(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return NotFound();

        var myLeaves = await _leaveRepository.GetByEmployeeIdAsync(userId, cancellationToken);

        var dto = new DashboardResponseDto
        {
            EmployeeName = user.FullName,
            PendingRequestsCount = myLeaves.Count(l => l.Status == LeaveStatus.Pending),
            ApprovedLeavesCount = myLeaves.Count(l => l.Status == LeaveStatus.Approved),
            RejectedLeavesCount = myLeaves.Count(l => l.Status == LeaveStatus.Rejected),
            TotalLeaveDaysRemaining = user.LeaveBalance
        };

        return Ok(dto);
    }

    // GET /api/Dashboard/admin-stats (HR & Team Lead Dashboard)
    [HttpGet("admin-stats")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<ActionResult<AdminDashboardResponseDto>> GetAdminDashboardStats(CancellationToken cancellationToken)
    {
        var allLeaves = (await _leaveRepository.GetAllAsync(cancellationToken)).ToList();
        var allUsers = await _userRepository.GetAllAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;

        var dto = new AdminDashboardResponseDto
        {
            TotalEmployees = allUsers.Count(),
            TotalRequestsCount = allLeaves.Count,
            PendingApprovalsCount = allLeaves.Count(l => l.Status == LeaveStatus.Pending),
            ApprovedRequestsCount = allLeaves.Count(l => l.Status == LeaveStatus.Approved),
            RejectedRequestsCount = allLeaves.Count(l => l.Status == LeaveStatus.Rejected),
            EmployeesCurrentlyOnLeave = allLeaves.Count(l =>
                l.Status == LeaveStatus.Approved &&
                l.StartDate.Date <= today &&
                l.EndDate.Date >= today)
        };

        return Ok(dto);
    }
}