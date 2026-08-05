using LeaveManagement.Application.Common.Helpers; 
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.DTOs.LeaveRequest;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestRepository _leaveRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveRequestsController(
        ILeaveRequestRepository leaveRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _leaveRepository = leaveRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID missing from token.");
        }

        return userId;
    }

    // GET ALL / PAGINATED & FILTERED LEAVE REQUESTS
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LeaveRequestQueryParameters query, CancellationToken cancellationToken)
    {
        // Non-managers are restricted to viewing only their own requests
        if (!User.IsInRole("Manager") && !User.IsInRole("Admin"))
        {
            query.EmployeeId = GetCurrentUserId();
        }

        var (items, totalCount) = await _leaveRepository.GetPagedAsync(
            query.EmployeeId,
            query.Status,
            query.SearchTerm,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Leave requests retrieved successfully.",
            data = items,
            pagination = new
            {
                totalCount,
                query.PageNumber,
                query.PageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
            }
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return NotFound(new { message = "Leave request not found." });
        return Ok(leave);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        // Calculate business days (excluding weekends)
        int businessDays = DateHelper.CalculateBusinessDays(dto.StartDate, dto.EndDate);

        if (businessDays <= 0)
        {
            return BadRequest(new { message = "The selected date range does not contain any official working days (Monday - Friday)." });
        }

        var applicant = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (applicant != null && applicant.LeaveBalance < businessDays)
        {
            return BadRequest(new { message = $"Insufficient leave balance. You requested {businessDays} working day(s), but only have {applicant.LeaveBalance} day(s) remaining." });
        }

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = currentUserId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            NumberOfDays = businessDays, 
            Reason = dto.Reason,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _leaveRepository.AddAsync(leaveRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = leaveRequest.Id }, leaveRequest);
    }

    // UPDATE PENDING LEAVE REQUEST
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLeaveRequest(Guid id, [FromBody] CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return NotFound(new { message = "Leave request not found." });

        var currentUserId = GetCurrentUserId();

        if (leave.EmployeeId != currentUserId)
        {
            return Forbid();
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return BadRequest(new { message = "Only pending leave requests can be updated." });
        }

        // Calculate business days (excluding weekends)
        int businessDays = DateHelper.CalculateBusinessDays(dto.StartDate, dto.EndDate);

        if (businessDays <= 0)
        {
            return BadRequest(new { message = "The selected date range does not contain any official working days (Monday - Friday)." });
        }

        leave.LeaveTypeId = dto.LeaveTypeId;
        leave.StartDate = dto.StartDate;
        leave.EndDate = dto.EndDate;
        leave.NumberOfDays = businessDays; 
        leave.Reason = dto.Reason;

        _leaveRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Leave request updated successfully.", data = leave });
    }

    // DELETE / CANCEL PENDING LEAVE REQUEST
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return NotFound(new { message = "Leave request not found." });

        var currentUserId = GetCurrentUserId();

        if (leave.EmployeeId != currentUserId && !User.IsInRole("Manager") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return BadRequest(new { message = "Cannot delete a request that has already been processed." });
        }

        _leaveRepository.Delete(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Leave request deleted successfully." });
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveLeave(Guid id, [FromBody] ManagerActionDto dto, CancellationToken cancellationToken)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return NotFound(new { message = "Leave request not found." });

        var currentUserId = GetCurrentUserId();

        if (leave.EmployeeId == currentUserId)
        {
            return BadRequest(new { message = "You cannot approve your own leave request. Another manager must review this request." });
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return BadRequest(new { message = "Only pending leave requests can be approved." });
        }

        leave.Status = LeaveStatus.Approved;
        leave.Approved = true;
        leave.ManagerComments = dto.Comments;

        var applicant = await _userRepository.GetByIdAsync(leave.EmployeeId, cancellationToken);
        if (applicant != null)
        {
            // Deducts business days from employee leave balance
            applicant.LeaveBalance -= leave.NumberOfDays;
            _userRepository.Update(applicant);
        }

        _leaveRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Leave request approved successfully." });
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RejectLeave(Guid id, [FromBody] ManagerActionDto dto, CancellationToken cancellationToken)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return NotFound(new { message = "Leave request not found." });

        var currentUserId = GetCurrentUserId();

        if (leave.EmployeeId == currentUserId)
        {
            return BadRequest(new { message = "You cannot reject your own leave request. Another manager must process this request." });
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return BadRequest(new { message = "Only pending leave requests can be rejected." });
        }

        leave.Status = LeaveStatus.Rejected;
        leave.Approved = false;
        leave.ManagerComments = dto.Comments;

        _leaveRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Leave request rejected." });
    }
}