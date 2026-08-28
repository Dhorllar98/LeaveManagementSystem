using LeaveManagement.Application.DTOs.LeaveAllocation;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveAllocationsController : BaseController
{
    private readonly ILeaveAllocationService _allocationService;
    private readonly IUserRepository _userRepository;

    public LeaveAllocationsController(
        ILeaveAllocationService allocationService,
        IUserRepository userRepository)
    {
        _allocationService = allocationService;
        _userRepository = userRepository;
    }

    private async Task<Guid?> GetUserOrganizationIdAsync(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return null;

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        return user?.OrganizationId;
    }

    [HttpGet]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<ActionResult<IEnumerable<LeaveAllocationDto>>> GetLeaveAllocations(CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var result = await _allocationService.GetAllocationsByOrganizationAsync(orgId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-balances")]
    public async Task<ActionResult<IEnumerable<UserLeaveBalanceDto>>> GetMyLeaveBalances([FromQuery] int? period, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        int targetPeriod = period ?? DateTime.UtcNow.Year;
        var balances = await _allocationService.GetUserLeaveBalancesAsync(currentUserId, orgId.Value, targetPeriod, cancellationToken);
        return Ok(balances);
    }

    [HttpGet("user-balances/{userId:guid}")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<ActionResult<IEnumerable<UserLeaveBalanceDto>>> GetUserLeaveBalances(Guid userId, [FromQuery] int? period, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        int targetPeriod = period ?? DateTime.UtcNow.Year;
        var balances = await _allocationService.GetUserLeaveBalancesAsync(userId, orgId.Value, targetPeriod, cancellationToken);
        return Ok(balances);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveAllocationDto>> GetLeaveAllocation(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var allocation = await _allocationService.GetAllocationByIdAsync(id, orgId.Value, cancellationToken);
        if (allocation == null) return NotFound();

        return Ok(allocation);
    }

    [HttpPost]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<ActionResult<LeaveAllocationDto>> CreateLeaveAllocation([FromBody] CreateLeaveAllocationDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var (success, errorMessage, data) = await _allocationService.CreateAllocationAsync(dto, orgId.Value, cancellationToken);
        if (!success) return BadRequest(new { message = errorMessage });

        return CreatedAtAction(nameof(GetLeaveAllocation), new { id = data!.Id }, data);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> UpdateLeaveAllocation(Guid id, UpdateLeaveAllocationDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var success = await _allocationService.UpdateAllocationAsync(id, dto, orgId.Value, cancellationToken);
        if (!success) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> DeleteLeaveAllocation(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var success = await _allocationService.DeleteAllocationAsync(id, orgId.Value, cancellationToken);
        if (!success) return NotFound();

        return NoContent();
    }
}