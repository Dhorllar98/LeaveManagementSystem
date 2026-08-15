using LeaveManagement.Application.DTOs.LeaveAllocation;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveAllocationsController : BaseController
{
    private readonly ILeaveAllocationRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveAllocationsController(
        ILeaveAllocationRepository repository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
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
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<ActionResult<IEnumerable<LeaveAllocationDto>>> GetLeaveAllocations(CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        var allocations = await _repository.GetAllByOrganizationAsync(orgId.Value, cancellationToken);
        var result = allocations.Select(la => new LeaveAllocationDto
        {
            Id = la.Id,
            NumberOfDays = la.NumberOfDays,
            Period = la.Period,
            EmployeeId = la.EmployeeId,
            EmployeeName = la.Employee?.FullName ?? string.Empty,
            LeaveTypeId = la.LeaveTypeId,
            LeaveTypeName = la.LeaveType?.Name ?? string.Empty
        });

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveAllocationDto>> GetLeaveAllocation(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);

        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null || allocation.Employee?.OrganizationId != orgId) return NotFound();

        return Ok(new LeaveAllocationDto
        {
            Id = allocation.Id,
            NumberOfDays = allocation.NumberOfDays,
            Period = allocation.Period,
            EmployeeId = allocation.EmployeeId,
            EmployeeName = allocation.Employee?.FullName ?? string.Empty,
            LeaveTypeId = allocation.LeaveTypeId,
            LeaveTypeName = allocation.LeaveType?.Name ?? string.Empty
        });
    }

    [HttpPost]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<ActionResult<LeaveAllocationDto>> CreateLeaveAllocation([FromBody] CreateLeaveAllocationDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);

        // Ensure the target employee belongs to the same organization
        var targetEmployee = await _userRepository.GetByIdAsync(dto.EmployeeId, cancellationToken);
        if (targetEmployee == null || targetEmployee.OrganizationId != orgId)
        {
            return BadRequest(new { message = "Invalid employee or employee does not belong to your organization." });
        }

        var exists = await _repository.AllocationExistsAsync(dto.EmployeeId, dto.LeaveTypeId, dto.Period, cancellationToken);
        if (exists)
        {
            return BadRequest(new { message = "An allocation for this leave type and period already exists for this employee." });
        }

        var allocation = new LeaveAllocation
        {
            NumberOfDays = dto.NumberOfDays,
            Period = dto.Period,
            EmployeeId = dto.EmployeeId,
            LeaveTypeId = dto.LeaveTypeId
        };

        await _repository.AddAsync(allocation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdAllocation = await _repository.GetByIdAsync(allocation.Id, cancellationToken);

        var result = new LeaveAllocationDto
        {
            Id = createdAllocation!.Id,
            NumberOfDays = createdAllocation.NumberOfDays,
            Period = createdAllocation.Period,
            EmployeeId = createdAllocation.EmployeeId,
            EmployeeName = createdAllocation.Employee?.FullName ?? string.Empty,
            LeaveTypeId = createdAllocation.LeaveTypeId,
            LeaveTypeName = createdAllocation.LeaveType?.Name ?? string.Empty
        };

        return CreatedAtAction(nameof(GetLeaveAllocation), new { id = allocation.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> UpdateLeaveAllocation(Guid id, UpdateLeaveAllocationDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);

        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null || allocation.Employee?.OrganizationId != orgId) return NotFound();

        allocation.NumberOfDays = dto.NumberOfDays;

        _repository.Update(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> DeleteLeaveAllocation(Guid id, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);

        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null || allocation.Employee?.OrganizationId != orgId) return NotFound();

        _repository.Delete(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}