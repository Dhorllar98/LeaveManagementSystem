using LeaveManagement.Application.DTOs.LeaveAllocation;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[Authorize]
public class LeaveAllocationsController : BaseController
{
    private readonly ILeaveAllocationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveAllocationsController(ILeaveAllocationRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IEnumerable<LeaveAllocationDto>>> GetLeaveAllocations(CancellationToken cancellationToken)
    {
        var allocations = await _repository.GetAllAsync(cancellationToken);
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
        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null) return NotFound();

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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<LeaveAllocationDto>> CreateLeaveAllocation([FromBody] CreateLeaveAllocationDto dto, CancellationToken cancellationToken)
    {
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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateLeaveAllocation(Guid id, UpdateLeaveAllocationDto dto, CancellationToken cancellationToken)
    {
        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null) return NotFound();

        allocation.NumberOfDays = dto.NumberOfDays;

        _repository.Update(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteLeaveAllocation(Guid id, CancellationToken cancellationToken)
    {
        var allocation = await _repository.GetByIdAsync(id, cancellationToken);
        if (allocation == null) return NotFound();

        _repository.Delete(allocation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}