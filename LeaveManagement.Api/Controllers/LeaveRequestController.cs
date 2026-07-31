using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[Authorize]
public class LeaveRequestsController : BaseController
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveRequestsController(
        ILeaveRepository leaveRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _leaveRepository = leaveRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetPaginatedLeaves(
        [FromQuery] Guid? employeeId,
        [FromQuery] LeaveStatus? status,
        [FromQuery] string? searchTerm,
        [FromQuery] string? sortBy,
        [FromQuery] bool isAscending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _leaveRepository.GetPaginatedLeavesAsync(
            employeeId, status, searchTerm, sortBy, isAscending, pageNumber, pageSize, cancellationToken);

        var result = items.Select(l => new LeaveResponseDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee?.FullName ?? string.Empty,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            DurationInDays = l.NumberOfDays,
            Reason = l.Reason ?? string.Empty, 
            Status = l.Status,
            ManagerComments = l.ManagerComments,
            CreatedAt = l.CreatedAt
        });

        return Ok(new { totalCount, pageNumber, pageSize, items = result });
    }

    [HttpGet("my-leaves")]
    public async Task<IActionResult> GetMyLeaves(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        if (employeeId == Guid.Empty) return Unauthorized();

        var leaves = await _leaveRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);
        var response = leaves.Select(l => new LeaveResponseDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee?.FullName ?? string.Empty,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            DurationInDays = l.NumberOfDays,
            Reason = l.Reason ?? string.Empty, 
            Status = l.Status,
            ManagerComments = l.ManagerComments,
            CreatedAt = l.CreatedAt
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveResponseDto>> GetLeaveById(Guid id, CancellationToken cancellationToken)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return NotFound();

        return Ok(new LeaveResponseDto
        {
            Id = leave.Id,
            EmployeeId = leave.EmployeeId,
            EmployeeName = leave.Employee?.FullName ?? string.Empty,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            DurationInDays = leave.NumberOfDays,
            Reason = leave.Reason ?? string.Empty, 
            Status = leave.Status,
            ManagerComments = leave.ManagerComments,
            CreatedAt = leave.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<LeaveResponseDto>> CreateLeave([FromBody] CreateLeaveDto dto, CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        if (employeeId == Guid.Empty) return Unauthorized();

        var days = (dto.EndDate - dto.StartDate).Days + 1;
        if (days <= 0) return BadRequest(new { message = "Invalid start and end dates." });
        var leave = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            NumberOfDays = days,
            Reason = dto.Reason ?? string.Empty, 
            Status = LeaveStatus.Pending,
            Approved = null
        };

        await _leaveRepository.AddAsync(leave, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdLeave = await _leaveRepository.GetByIdAsync(leave.Id, cancellationToken);
        if (createdLeave == null) return NotFound(new { message = "Failed to retrieve created leave request." });

        return CreatedAtAction(nameof(GetLeaveById), new { id = createdLeave.Id }, new LeaveResponseDto
        {
            Id = createdLeave.Id,
            EmployeeId = createdLeave.EmployeeId,
            EmployeeName = createdLeave.Employee?.FullName ?? string.Empty,
            StartDate = createdLeave.StartDate,
            EndDate = createdLeave.EndDate,
            DurationInDays = createdLeave.NumberOfDays,
            Reason = createdLeave.Reason ?? string.Empty,
            Status = createdLeave.Status,
            ManagerComments = createdLeave.ManagerComments,
            CreatedAt = createdLeave.CreatedAt
        });
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveLeave(Guid id, [FromBody] ManagerActionDto dto, CancellationToken cancellationToken)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return NotFound();

        if (leave.Status != LeaveStatus.Pending)
            return BadRequest(new { message = "Only pending leave requests can be approved." });

        leave.Status = LeaveStatus.Approved;
        leave.Approved = true;
        leave.ManagerComments = dto.Comments;

        var user = await _userRepository.GetByIdAsync(leave.EmployeeId, cancellationToken);
        if (user != null)
        {
            user.LeaveBalance -= leave.NumberOfDays;
            _userRepository.Update(user);
        }

        _leaveRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RejectLeave(Guid id, [FromBody] ManagerActionDto dto, CancellationToken cancellationToken)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return NotFound();

        if (leave.Status != LeaveStatus.Pending)
            return BadRequest(new { message = "Only pending leave requests can be rejected." });

        leave.Status = LeaveStatus.Rejected;
        leave.Approved = false;
        leave.ManagerComments = dto.Comments;

        _leaveRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}