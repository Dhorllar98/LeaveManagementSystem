using LeaveManagement.Application.DTOs.Dashboard;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IUserRepository _userRepository;

    public DashboardController(ILeaveRepository leaveRepository, IUserRepository userRepository)
    {
        _leaveRepository = leaveRepository;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboardStats(CancellationToken cancellationToken)
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
}