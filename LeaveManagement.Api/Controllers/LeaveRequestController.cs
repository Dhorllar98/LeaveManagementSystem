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

        var formattedItems = items.Select(l => new
        {
            id = l.Id,
            employeeId = l.EmployeeId,
            employeeName = l.Employee?.FullName ?? "N/A",
            department = !string.IsNullOrWhiteSpace(l.Employee?.Department) ? l.Employee.Department : "Unassigned",
            leaveTypeId = l.LeaveTypeId,

            leaveType = l.LeaveType?.Name ?? "N/A",
            leaveTypeName = l.LeaveType?.Name ?? "N/A",
            type = l.LeaveType?.Name ?? "N/A",

            leaveTypeDetails = new
            {
                id = l.LeaveTypeId,
                name = l.LeaveType?.Name ?? "N/A"
            },

            startDate = l.StartDate,
            endDate = l.EndDate,
            numberOfDays = l.NumberOfDays,
            reason = l.Reason,
            status = l.Status.ToString(),
            createdAt = l.CreatedAt,
            managerComments = l.ManagerComments
        });

        return Ok(new
        {
            success = true,
            message = "Leave requests retrieved successfully.",
            data = formattedItems,
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

        return Ok(new
        {
            id = leave.Id,
            employeeId = leave.EmployeeId,
            employeeName = leave.Employee?.FullName ?? "N/A",
            department = !string.IsNullOrWhiteSpace(leave.Employee?.Department) ? leave.Employee.Department : "Unassigned",
            leaveTypeId = leave.LeaveTypeId,

            leaveType = leave.LeaveType?.Name ?? "N/A",
            leaveTypeName = leave.LeaveType?.Name ?? "N/A",
            type = leave.LeaveType?.Name ?? "N/A",

            leaveTypeDetails = new
            {
                id = leave.LeaveTypeId,
                name = leave.LeaveType?.Name ?? "N/A"
            },

            startDate = leave.StartDate,
            endDate = leave.EndDate,
            numberOfDays = leave.NumberOfDays,
            reason = leave.Reason,
            status = leave.Status.ToString(),
            createdAt = leave.CreatedAt,
            managerComments = leave.ManagerComments
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

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

        var responseData = new
        {
            id = leaveRequest.Id,
            employeeId = leaveRequest.EmployeeId,
            leaveTypeId = leaveRequest.LeaveTypeId,
            startDate = leaveRequest.StartDate,
            endDate = leaveRequest.EndDate,
            numberOfDays = leaveRequest.NumberOfDays,
            reason = leaveRequest.Reason,
            status = leaveRequest.Status.ToString(),
            createdAt = leaveRequest.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = leaveRequest.Id }, new
        {
            success = true,
            message = "Leave request submitted successfully.",
            data = responseData
        });
    }

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