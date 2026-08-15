using LeaveManagement.Application.Common.Helpers;
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.DTOs.LeaveRequest;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using LeaveManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : BaseController
{
    private readonly ILeaveRequestRepository _leaveRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<LeaveRequestsController> _logger;

    public LeaveRequestsController(
        ILeaveRequestRepository leaveRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        AppDbContext context,
        IEmailService emailService,
        ILogger<LeaveRequestsController> logger)
    {
        _leaveRepository = leaveRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    private async Task<Guid?> GetUserOrganizationIdAsync(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return null;

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        return user?.OrganizationId;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] LeaveRequestQueryParameters query, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        if (!User.IsInRole("TeamLead") && !User.IsInRole("HR"))
        {
            query.EmployeeId = GetCurrentUserId();
        }

        var (items, totalCount) = await _leaveRepository.GetPagedAsync(
            orgId.Value,
            query.EmployeeId,
            query.Status,
            query.SearchTerm,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var formattedItems = items.Select(l => new LeaveRequestSummaryDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee?.FullName ?? "N/A",
            Department = l.Employee?.Department?.Name ?? "Unassigned",
            LeaveTypeId = l.LeaveTypeId,
            LeaveTypeName = l.LeaveType?.Name ?? "N/A",
            LeaveType = l.LeaveType != null ? new LeaveTypeSummaryDto
            {
                Id = l.LeaveType.Id,
                Name = l.LeaveType.Name
            } : null,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            NumberOfDays = l.NumberOfDays,
            Reason = l.Reason ?? string.Empty,
            Status = l.Status.ToString(),
            ManagerComments = l.ManagerComments,
            CreatedAt = l.CreatedAt
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

    // HR DEDICATED COMPANY OVERVIEW
    [HttpGet("all")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> GetTotalRequestsForHR([FromQuery] LeaveRequestQueryParameters query, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        if (orgId == null) return BadRequest(new { message = "Organization not found for current user." });

        query.EmployeeId = null;

        var (items, totalCount) = await _leaveRepository.GetPagedAsync(
            orgId.Value,
            query.EmployeeId,
            query.Status,
            query.SearchTerm,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var formattedItems = items.Select(l => new LeaveRequestSummaryDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee?.FullName ?? "N/A",
            Department = l.Employee?.Department?.Name ?? "Unassigned",
            LeaveTypeId = l.LeaveTypeId,
            LeaveTypeName = l.LeaveType?.Name ?? "N/A",
            LeaveType = l.LeaveType != null ? new LeaveTypeSummaryDto
            {
                Id = l.LeaveType.Id,
                Name = l.LeaveType.Name
            } : null,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            NumberOfDays = l.NumberOfDays,
            Reason = l.Reason ?? string.Empty,
            Status = l.Status.ToString(),
            ManagerComments = l.ManagerComments,
            CreatedAt = l.CreatedAt
        });

        return Ok(new
        {
            success = true,
            message = "Total company leave requests retrieved for HR overview.",
            totalCount,
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
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);

        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null || leave.OrganizationId != orgId && leave.Employee?.OrganizationId != orgId)
            return NotFound(new { message = "Leave request not found." });

        var summary = new LeaveRequestSummaryDto
        {
            Id = leave.Id,
            EmployeeId = leave.EmployeeId,
            EmployeeName = leave.Employee?.FullName ?? "N/A",
            Department = leave.Employee?.Department?.Name ?? "Unassigned",
            LeaveTypeId = leave.LeaveTypeId,
            LeaveTypeName = leave.LeaveType?.Name ?? "N/A",
            LeaveType = leave.LeaveType != null ? new LeaveTypeSummaryDto
            {
                Id = leave.LeaveType.Id,
                Name = leave.LeaveType.Name
            } : null,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            NumberOfDays = leave.NumberOfDays,
            Reason = leave.Reason ?? string.Empty,
            Status = leave.Status.ToString(),
            ManagerComments = leave.ManagerComments,
            CreatedAt = leave.CreatedAt
        };

        return Ok(summary);
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
            return BadRequest(new { message = $"Insufficient leave balance. You requested {businessDays} working day(s), but only have {Math.Max(0, applicant.LeaveBalance)} day(s) remaining." });
        }

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = currentUserId,
            OrganizationId = applicant?.OrganizationId,
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

        if (applicant != null && applicant.OrganizationId.HasValue)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var settings = await _context.NotificationSettings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.OrganizationId == applicant.OrganizationId.Value);

                    if (settings == null || settings.EnableNewLeaveRequestEmails)
                    {
                        var hrAndTeamLeadEmails = await _context.Users
                            .Where(u => u.OrganizationId == applicant.OrganizationId.Value &&
                                        (u.Role == UserRole.HR || u.Id == applicant.TeamLeadId))
                            .Select(u => u.Email)
                            .Distinct()
                            .ToListAsync();

                        string subject = $"New Leave Request - {applicant.FullName}";
                        string body = $@"
                            <h3>New Leave Request Submitted</h3>
                            <p><strong>Employee:</strong> {applicant.FullName} ({applicant.EmployeeCode ?? "N/A"})</p>
                            <p><strong>Working Days:</strong> {businessDays}</p>
                            <p><strong>Dates:</strong> {dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}</p>
                            <p><strong>Reason:</strong> {dto.Reason}</p>
                            <p>Log in to LeaveFlow to review and process this request.</p>";

                        foreach (var email in hrAndTeamLeadEmails)
                        {
                            await _emailService.SendEmailAsync(email, subject, body);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending new leave request notification email.");
                }
            }, CancellationToken.None);
        }

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
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);

        if (leave == null || leave.OrganizationId != orgId && leave.Employee?.OrganizationId != orgId)
            return NotFound(new { message = "Leave request not found." });

        var currentUserId = GetCurrentUserId();

        if (leave.EmployeeId != currentUserId && !User.IsInRole("TeamLead") && !User.IsInRole("HR"))
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
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> ApproveLeave(Guid id, [FromBody] ManagerActionDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);

        if (leave == null || leave.OrganizationId != orgId && leave.Employee?.OrganizationId != orgId)
            return NotFound(new { message = "Leave request not found." });

        var currentUserId = GetCurrentUserId();

        if (leave.EmployeeId == currentUserId)
        {
            return BadRequest(new { message = "You cannot approve your own leave request." });
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return BadRequest(new { message = "Only pending leave requests can be approved." });
        }

        var applicant = await _userRepository.GetByIdAsync(leave.EmployeeId, cancellationToken);
        if (applicant != null)
        {
            if (applicant.LeaveBalance < leave.NumberOfDays)
            {
                return BadRequest(new
                {
                    message = $"Approval failed. The employee requested {leave.NumberOfDays} day(s), but only has {Math.Max(0, applicant.LeaveBalance)} day(s) remaining."
                });
            }

            applicant.LeaveBalance = Math.Max(0, applicant.LeaveBalance - leave.NumberOfDays);
            _userRepository.Update(applicant);
        }

        leave.Status = LeaveStatus.Approved;
        leave.Approved = true;
        leave.ManagerComments = dto.Comments;

        _leaveRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (applicant != null && applicant.OrganizationId.HasValue)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var settings = await _context.NotificationSettings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.OrganizationId == applicant.OrganizationId.Value);

                    if (settings == null || settings.EnableLeaveStatusUpdateEmails)
                    {
                        string subject = "Leave Request Approved - LeaveFlow";
                        string body = $@"
                            <h3>Your Leave Request Has Been Approved!</h3>
                            <p>Hi {applicant.FullName},</p>
                            <p>Your leave request for <strong>{leave.StartDate:yyyy-MM-dd}</strong> to <strong>{leave.EndDate:yyyy-MM-dd}</strong> ({leave.NumberOfDays} working days) has been <strong style='color: green;'>APPROVED</strong>.</p>
                            <p><strong>Comments:</strong> {dto.Comments ?? "None"}</p>
                            <p>Your remaining leave balance is <strong>{applicant.LeaveBalance}</strong> days.</p>";

                        await _emailService.SendEmailAsync(applicant.Email, subject, body);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending approval email to employee.");
                }
            }, CancellationToken.None);
        }

        return Ok(new { message = "Leave request approved successfully." });
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> RejectLeave(Guid id, [FromBody] ManagerActionDto dto, CancellationToken cancellationToken)
    {
        var orgId = await GetUserOrganizationIdAsync(cancellationToken);
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);

        if (leave == null || leave.OrganizationId != orgId && leave.Employee?.OrganizationId != orgId)
            return NotFound(new { message = "Leave request not found." });

        var currentUserId = GetCurrentUserId();

        if (leave.EmployeeId == currentUserId)
        {
            return BadRequest(new { message = "You cannot reject your own leave request." });
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

        var applicant = await _userRepository.GetByIdAsync(leave.EmployeeId, cancellationToken);

        if (applicant != null && applicant.OrganizationId.HasValue)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var settings = await _context.NotificationSettings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.OrganizationId == applicant.OrganizationId.Value);

                    if (settings == null || settings.EnableLeaveStatusUpdateEmails)
                    {
                        string subject = "Leave Request Update - LeaveFlow";
                        string body = $@"
                            <h3>Your Leave Request Status Update</h3>
                            <p>Hi {applicant.FullName},</p>
                            <p>Your leave request for <strong>{leave.StartDate:yyyy-MM-dd}</strong> to <strong>{leave.EndDate:yyyy-MM-dd}</strong> has been <strong style='color: red;'>REJECTED</strong>.</p>
                            <p><strong>Reason / Comments:</strong> {dto.Comments ?? "No comments provided."}</p>";

                        await _emailService.SendEmailAsync(applicant.Email, subject, body);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending rejection email to employee.");
                }
            }, CancellationToken.None);
        }

        return Ok(new { message = "Leave request rejected." });
    }
}