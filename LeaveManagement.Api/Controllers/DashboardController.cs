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

    [HttpGet("admin-stats")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> GetAdminDashboardStats(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        // 1. Resolve the admin's organization
        var adminUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (adminUser?.OrganizationId == null)
        {
            return BadRequest(new { message = "User organization not found." });
        }

        var orgId = adminUser.OrganizationId.Value;

        // 2. Fetch stats ONLY for this organization
        var orgLeaves = await _leaveRepository.GetAllByOrganizationAsync(orgId, cancellationToken);
        var orgUsers = await _userRepository.GetAllByOrganizationAsync(orgId, cancellationToken);

        // 3. Calculate statistics safely scoped to the tenant
        var pendingApprovals = orgLeaves.Count(l => l.Status == LeaveStatus.Pending);
        var totalEmployees = orgUsers.Count();
        var activeLeaves = orgLeaves.Count(l => l.Status == LeaveStatus.Approved && l.StartDate <= DateTime.UtcNow && l.EndDate >= DateTime.UtcNow);

        return Ok(new
        {
            success = true,
            data = new
            {
                TotalEmployees = totalEmployees,
                PendingLeaveApprovals = pendingApprovals,
                ActiveLeavesToday = activeLeaves
            }
        });
    }
}