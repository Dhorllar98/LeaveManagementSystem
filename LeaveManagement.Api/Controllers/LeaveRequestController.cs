using LeaveManagement.Application.DTOs.LeaveRequest;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[Authorize]
public class LeaveRequestsController : BaseController
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveRequestsController(
        ILeaveRequestRepository leaveRequestRepository,
        IUnitOfWork unitOfWork)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequests(CancellationToken cancellationToken)
    {
        var requests = await _leaveRequestRepository.GetAllAsync(cancellationToken);

        var response = requests.Select(lr => new LeaveRequestDto
        {
            Id = lr.Id,
            EmployeeId = lr.EmployeeId,
            EmployeeName = lr.Employee?.FullName ?? string.Empty,
            LeaveTypeId = lr.LeaveTypeId,
            LeaveTypeName = lr.LeaveType?.Name ?? string.Empty,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            NumberOfDays = lr.NumberOfDays,
            Approved = lr.Approved,
            Cancelled = lr.Cancelled,
            RequestComments = lr.RequestComments
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveRequestDto>> GetLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var lr = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken);
        if (lr == null) return NotFound();

        var response = new LeaveRequestDto
        {
            Id = lr.Id,
            EmployeeId = lr.EmployeeId,
            EmployeeName = lr.Employee?.FullName ?? string.Empty,
            LeaveTypeId = lr.LeaveTypeId,
            LeaveTypeName = lr.LeaveType?.Name ?? string.Empty,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            NumberOfDays = lr.NumberOfDays,
            Approved = lr.Approved,
            Cancelled = lr.Cancelled,
            RequestComments = lr.RequestComments
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        if (employeeId == Guid.Empty)
        {
            return Unauthorized();
        }

        var days = (dto.EndDate - dto.StartDate).Days + 1;
        if (days <= 0)
        {
            return BadRequest(new { message = "Invalid start and end dates." });
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            NumberOfDays = days,
            RequestComments = dto.RequestComments,
            Reason = dto.RequestComments ?? "No reason provided", // <-- Fixes the NOT NULL constraint error
            Approved = null, // Pending
            Status = LeaveStatus.Pending,
            Cancelled = false
        };

        await _leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdRequest = await _leaveRequestRepository.GetByIdAsync(leaveRequest.Id, cancellationToken);

        var result = new LeaveRequestDto
        {
            Id = createdRequest!.Id,
            EmployeeId = createdRequest.EmployeeId,
            EmployeeName = createdRequest.Employee?.FullName ?? string.Empty,
            LeaveTypeId = createdRequest.LeaveTypeId,
            LeaveTypeName = createdRequest.LeaveType?.Name ?? string.Empty,
            StartDate = createdRequest.StartDate,
            EndDate = createdRequest.EndDate,
            NumberOfDays = createdRequest.NumberOfDays,
            Approved = createdRequest.Approved,
            Cancelled = createdRequest.Cancelled,
            RequestComments = createdRequest.RequestComments
        };

        return CreatedAtAction(nameof(GetLeaveRequest), new { id = leaveRequest.Id }, result);
    }
}