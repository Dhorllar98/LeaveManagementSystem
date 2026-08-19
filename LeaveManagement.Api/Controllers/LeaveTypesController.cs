using LeaveManagement.Application.DTOs.LeaveType;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveTypesController : BaseController
{
    private readonly ILeaveTypeService _leaveTypeService;
    private readonly IUserRepository _userRepository;

    public LeaveTypesController(ILeaveTypeService leaveTypeService, IUserRepository userRepository)
    {
        _leaveTypeService = leaveTypeService;
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
    public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetLeaveTypes(CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var result = await _leaveTypeService.GetLeaveTypesAsync(orgId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveTypeDto>> GetLeaveType(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var leaveType = await _leaveTypeService.GetLeaveTypeByIdAsync(id, orgId.Value, cancellationToken);
        if (leaveType == null) return NotFound();

        return Ok(leaveType);
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<LeaveTypeDto>> CreateLeaveType(CreateLeaveTypeDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var result = await _leaveTypeService.CreateLeaveTypeAsync(dto, orgId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetLeaveType), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> UpdateLeaveType(Guid id, UpdateLeaveTypeDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var success = await _leaveTypeService.UpdateLeaveTypeAsync(id, dto, orgId.Value, cancellationToken);
        if (!success) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> DeleteLeaveType(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var success = await _leaveTypeService.DeleteLeaveTypeAsync(id, orgId.Value, cancellationToken);
        if (!success) return NotFound();

        return NoContent();
    }
}