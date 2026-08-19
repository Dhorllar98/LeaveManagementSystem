using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.DTOs.LeaveRequest;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : BaseController
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LeaveRequestQueryParameters query, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        bool isLeadOrHr = User.IsInRole("TeamLead") || User.IsInRole("HR");

        var result = await _leaveRequestService.GetPagedLeaveRequestsAsync(currentUserId, isLeadOrHr, query, cancellationToken);
        if (result == null) return BadRequest(new { message = "Organization not found for current user." });

        var (items, totalCount) = result.Value;

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

    [HttpGet("all")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetTotalRequestsForHR([FromQuery] LeaveRequestQueryParameters query, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _leaveRequestService.GetTotalRequestsForHrAsync(currentUserId, query, cancellationToken);
        if (result == null) return BadRequest(new { message = "Organization not found for current user." });

        var (items, totalCount) = result.Value;

        return Ok(new
        {
            success = true,
            message = "Total company leave requests retrieved for HR overview.",
            totalCount,
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
        var currentUserId = GetCurrentUserId();
        var summary = await _leaveRequestService.GetByIdAsync(id, currentUserId, cancellationToken);
        if (summary == null) return NotFound(new { message = "Leave request not found." });

        return Ok(summary);
    }

    [HttpPost]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var (success, message, data, statusCode) = await _leaveRequestService.CreateLeaveRequestAsync(currentUserId, dto, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                401 => Unauthorized(new { message }),
                _ => BadRequest(new { message })
            };
        }

        var requestId = data?.GetType().GetProperty("id")?.GetValue(data, null);

        return CreatedAtAction(nameof(GetById), new { id = requestId }, new
        {
            success = true,
            message,
            data
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLeaveRequest(Guid id, [FromBody] CreateLeaveRequestDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var (success, message, statusCode, data) = await _leaveRequestService.UpdateLeaveRequestAsync(id, currentUserId, dto, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                404 => NotFound(new { message }),
                403 => Forbid(),
                _ => BadRequest(new { message })
            };
        }

        return Ok(new { message, data });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        bool isLeadOrHr = User.IsInRole("TeamLead") || User.IsInRole("HR");

        var (success, message, statusCode) = await _leaveRequestService.DeleteLeaveRequestAsync(id, currentUserId, isLeadOrHr, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                404 => NotFound(new { message }),
                403 => Forbid(),
                _ => BadRequest(new { message })
            };
        }

        return Ok(new { message });
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> ApproveLeave(Guid id, [FromBody] ManagerActionDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var (success, message, statusCode) = await _leaveRequestService.ApproveLeaveAsync(id, currentUserId, dto, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                404 => NotFound(new { message }),
                _ => BadRequest(new { message })
            };
        }

        return Ok(new { message });
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> RejectLeave(Guid id, [FromBody] ManagerActionDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var (success, message, statusCode) = await _leaveRequestService.RejectLeaveAsync(id, currentUserId, dto, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                404 => NotFound(new { message }),
                _ => BadRequest(new { message })
            };
        }

        return Ok(new { message });
    }
}