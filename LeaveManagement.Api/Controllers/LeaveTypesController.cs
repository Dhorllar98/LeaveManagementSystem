using LeaveManagement.Application.DTOs.LeaveType;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveTypesController : BaseController
{
    private readonly ILeaveTypeRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveTypesController(ILeaveTypeRepository repository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
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

        var leaveTypes = await _repository.GetAllByOrganizationAsync(orgId.Value, cancellationToken);
        var result = leaveTypes.Select(lt => new LeaveTypeDto
        {
            Id = lt.Id,
            Name = lt.Name,
            DefaultDays = lt.DefaultDays
        });

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveTypeDto>> GetLeaveType(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);

        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveType == null || leaveType.OrganizationId != orgId) return NotFound();

        return Ok(new LeaveTypeDto
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            DefaultDays = leaveType.DefaultDays
        });
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<ActionResult<LeaveTypeDto>> CreateLeaveType(CreateLeaveTypeDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var leaveType = new LeaveType
        {
            OrganizationId = orgId.Value,
            Name = dto.Name,
            DefaultDays = dto.DefaultDays
        };

        await _repository.AddAsync(leaveType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new LeaveTypeDto
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            DefaultDays = leaveType.DefaultDays
        };

        return CreatedAtAction(nameof(GetLeaveType), new { id = leaveType.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> UpdateLeaveType(Guid id, UpdateLeaveTypeDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);

        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveType == null || leaveType.OrganizationId != orgId) return NotFound();

        leaveType.Name = dto.Name;
        leaveType.DefaultDays = dto.DefaultDays;
        leaveType.UpdatedAt = DateTime.UtcNow;

        _repository.Update(leaveType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> DeleteLeaveType(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);

        var leaveType = await _repository.GetByIdAsync(id, cancellationToken);
        if (leaveType == null || leaveType.OrganizationId != orgId) return NotFound();

        _repository.Delete(leaveType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}