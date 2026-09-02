using LeaveManagement.Application.Common.Helpers;
using LeaveManagement.Application.DTOs.Leave;
using LeaveManagement.Application.DTOs.LeaveRequest;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Application.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaveRequestService> _logger;

    public LeaveRequestService(
        ILeaveRequestRepository leaveRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAppDbContext context,
        IEmailService emailService,
        IServiceScopeFactory scopeFactory,
        ILogger<LeaveRequestService> logger)
    {
        _leaveRepository = leaveRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _context = context;
        _emailService = emailService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private async Task<Guid?> GetUserOrgIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user?.OrganizationId;
    }

    private async Task<HashSet<DateTime>> GetPublicHolidaysAsync(Guid orgId, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var dates = await _context.PublicHolidays
            .AsNoTracking()
            .Where(ph => ph.OrganizationId == orgId && ph.Date.Date >= start.Date && ph.Date.Date <= end.Date)
            .Select(ph => ph.Date.Date)
            .ToListAsync(cancellationToken);

        return new HashSet<DateTime>(dates);
    }

    public async Task<IEnumerable<LeaveRequestSummaryDto>> GetOnLeaveTodayAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrgIdAsync(userId, cancellationToken);
        if (orgId == null) return Enumerable.Empty<LeaveRequestSummaryDto>();

        var today = DateTime.UtcNow.Date;

        var activeLeaves = await _context.LeaveRequests
            .AsNoTracking()
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .Include(l => l.LeaveType)
            .Include(l => l.HandoverUser)
            .Where(l => l.OrganizationId == orgId &&
                        l.Status == LeaveStatus.Approved &&
                        l.StartDate.Date <= today &&
                        l.EndDate.Date >= today)
            .ToListAsync(cancellationToken);

        return MapToSummaryDtos(activeLeaves);
    }

    public async Task<IEnumerable<LeaveRequestSummaryDto>> GetApprovedLeaveRequestsAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrgIdAsync(userId, cancellationToken);
        if (orgId == null) return Enumerable.Empty<LeaveRequestSummaryDto>();

        var query = _context.LeaveRequests
            .AsNoTracking()
            .Include(l => l.Employee).ThenInclude(e => e.Department)
            .Include(l => l.LeaveType)
            .Include(l => l.HandoverUser)
            .Where(l => l.OrganizationId == orgId && l.Status == LeaveStatus.Approved);

        if (startDate.HasValue)
            query = query.Where(l => l.EndDate.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(l => l.StartDate.Date <= endDate.Value.Date);

        var approvedLeaves = await query
            .OrderByDescending(l => l.StartDate)
            .ToListAsync(cancellationToken);

        return MapToSummaryDtos(approvedLeaves);
    }

    public async Task<(IEnumerable<LeaveRequestSummaryDto> Items, int TotalCount)?> GetPagedLeaveRequestsAsync(
        Guid userId,
        bool isLeadOrHr,
        LeaveRequestQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrgIdAsync(userId, cancellationToken);
        if (orgId == null) return null;

        if (!isLeadOrHr)
        {
            query.EmployeeId = userId;
        }

        var (items, totalCount) = await _leaveRepository.GetPagedAsync(
            orgId.Value,
            query.EmployeeId,
            query.Status,
            query.SearchTerm,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var formattedItems = MapToSummaryDtos(items);
        return (formattedItems, totalCount);
    }

    public async Task<(IEnumerable<LeaveRequestSummaryDto> Items, int TotalCount)?> GetTotalRequestsForHrAsync(
        Guid userId,
        LeaveRequestQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrgIdAsync(userId, cancellationToken);
        if (orgId == null) return null;

        query.EmployeeId = null;

        var (items, totalCount) = await _leaveRepository.GetPagedAsync(
            orgId.Value,
            query.EmployeeId,
            query.Status,
            query.SearchTerm,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var formattedItems = MapToSummaryDtos(items);
        return (formattedItems, totalCount);
    }

    public async Task<LeaveRequestSummaryDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrgIdAsync(userId, cancellationToken);
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);

        if (leave == null || (leave.OrganizationId != orgId && leave.Employee?.OrganizationId != orgId))
            return null;

        return MapToSummaryDto(leave);
    }

    public async Task<(bool Success, string Message, object? Data, int StatusCode)> CreateLeaveRequestAsync(
        Guid userId,
        CreateLeaveRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var applicant = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (applicant == null)
            return (false, "User record not found.", null, 401);

        if (!applicant.OrganizationId.HasValue)
            return (false, "User is not assigned to an organization. Request cannot be processed.", null, 400);

        var leaveTypeExists = await _context.LeaveTypes
            .AnyAsync(lt => lt.Id == dto.LeaveTypeId, cancellationToken);
        if (!leaveTypeExists)
            return (false, "The selected leave type does not exist.", null, 400);

        if (dto.HandoverUserId.HasValue)
        {
            if (dto.HandoverUserId.Value == userId)
                return (false, "You cannot select yourself as the handover colleague.", null, 400);

            var handoverUser = await _userRepository.GetByIdAsync(dto.HandoverUserId.Value, cancellationToken);
            if (handoverUser == null || handoverUser.DepartmentId != applicant.DepartmentId)
                return (false, "The selected handover colleague must belong to your department.", null, 400);
        }

        var publicHolidays = await GetPublicHolidaysAsync(applicant.OrganizationId.Value, dto.StartDate, dto.EndDate, cancellationToken);
        int businessDays = DateHelper.CalculateBusinessDays(dto.StartDate, dto.EndDate, publicHolidays);

        if (businessDays <= 0)
            return (false, "The selected date range contains no official working days (excludes weekends and public holidays).", null, 400);

        if (applicant.LeaveBalance < businessDays)
            return (false, $"Insufficient leave balance. You requested {businessDays} working day(s), but only have {Math.Max(0, applicant.LeaveBalance)} day(s) remaining.", null, 400);

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId,
            OrganizationId = applicant.OrganizationId.Value,
            LeaveTypeId = dto.LeaveTypeId,
            HandoverUserId = dto.HandoverUserId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            NumberOfDays = businessDays,
            Reason = dto.Reason,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _leaveRepository.AddAsync(leaveRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        DispatchNewLeaveRequestEmail(applicant, businessDays, dto);

        var responseData = new
        {
            id = leaveRequest.Id,
            employeeId = leaveRequest.EmployeeId,
            leaveTypeId = leaveRequest.LeaveTypeId,
            handoverUserId = leaveRequest.HandoverUserId,
            startDate = leaveRequest.StartDate,
            endDate = leaveRequest.EndDate,
            numberOfDays = leaveRequest.NumberOfDays,
            reason = leaveRequest.Reason,
            status = leaveRequest.Status.ToString(),
            createdAt = leaveRequest.CreatedAt
        };

        return (true, "Leave request submitted successfully.", responseData, 201);
    }

    public async Task<(bool Success, string Message, int StatusCode, object? Data)> UpdateLeaveRequestAsync(
        Guid id,
        Guid userId,
        CreateLeaveRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (leave == null) return (false, "Leave request not found.", 404, null);

        if (leave.EmployeeId != userId) return (false, "Forbidden", 403, null);

        if (leave.Status != LeaveStatus.Pending)
            return (false, "Only pending leave requests can be updated.", 400, null);

        var applicant = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (dto.HandoverUserId.HasValue)
        {
            if (dto.HandoverUserId.Value == userId)
                return (false, "You cannot select yourself as the handover colleague.", 400, null);

            var handoverUser = await _userRepository.GetByIdAsync(dto.HandoverUserId.Value, cancellationToken);
            if (handoverUser == null || handoverUser.DepartmentId != applicant?.DepartmentId)
                return (false, "The selected handover colleague must belong to your department.", 400, null);
        }

        var publicHolidays = await GetPublicHolidaysAsync(leave.OrganizationId!.Value, dto.StartDate, dto.EndDate, cancellationToken);
        int businessDays = DateHelper.CalculateBusinessDays(dto.StartDate, dto.EndDate, publicHolidays);

        if (businessDays <= 0)
            return (false, "The selected date range contains no official working days.", 400, null);

        leave.LeaveTypeId = dto.LeaveTypeId;
        leave.HandoverUserId = dto.HandoverUserId;
        leave.StartDate = dto.StartDate;
        leave.EndDate = dto.EndDate;
        leave.NumberOfDays = businessDays;
        leave.Reason = dto.Reason;

        _leaveRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, "Leave request updated successfully.", 200, leave);
    }

    public async Task<(bool Success, string Message, int StatusCode)> DeleteLeaveRequestAsync(
        Guid id,
        Guid userId,
        bool isLeadOrHr,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrgIdAsync(userId, cancellationToken);
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);

        if (leave == null || (leave.OrganizationId != orgId && leave.Employee?.OrganizationId != orgId))
            return (false, "Leave request not found.", 404);

        if (leave.EmployeeId != userId && !isLeadOrHr) return (false, "Forbidden", 403);

        if (leave.Status != LeaveStatus.Pending)
            return (false, "Cannot delete a request that has already been processed.", 400);

        _leaveRepository.Delete(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, "Leave request deleted successfully.", 200);
    }

    public async Task<(bool Success, string Message, int StatusCode)> ApproveLeaveAsync(
        Guid id,
        Guid currentUserId,
        ManagerActionDto dto,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrgIdAsync(currentUserId, cancellationToken);
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);

        if (leave == null || (leave.OrganizationId != orgId && leave.Employee?.OrganizationId != orgId))
            return (false, "Leave request not found.", 404);

        if (leave.EmployeeId == currentUserId)
            return (false, "You cannot approve your own leave request.", 400);

        if (leave.Status != LeaveStatus.Pending)
            return (false, "Only pending leave requests can be approved.", 400);

        var applicant = await _userRepository.GetByIdAsync(leave.EmployeeId, cancellationToken);
        if (applicant != null)
        {
            if (applicant.LeaveBalance < leave.NumberOfDays)
            {
                return (false, $"Approval failed. The employee requested {leave.NumberOfDays} day(s), but only has {Math.Max(0, applicant.LeaveBalance)} day(s) remaining.", 400);
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
            DispatchApprovalEmail(applicant, leave, dto.Comments);
        }

        return (true, "Leave request approved successfully.", 200);
    }

    public async Task<(bool Success, string Message, int StatusCode)> RejectLeaveAsync(
        Guid id,
        Guid currentUserId,
        ManagerActionDto dto,
        CancellationToken cancellationToken = default)
    {
        var orgId = await GetUserOrgIdAsync(currentUserId, cancellationToken);
        var leave = await _leaveRepository.GetByIdAsync(id, cancellationToken);

        if (leave == null || (leave.OrganizationId != orgId && leave.Employee?.OrganizationId != orgId))
            return (false, "Leave request not found.", 404);

        if (leave.EmployeeId == currentUserId)
            return (false, "You cannot reject your own leave request.", 400);

        if (leave.Status != LeaveStatus.Pending)
            return (false, "Only pending leave requests can be rejected.", 400);

        leave.Status = LeaveStatus.Rejected;
        leave.Approved = false;
        leave.ManagerComments = dto.Comments;

        _leaveRepository.Update(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var applicant = await _userRepository.GetByIdAsync(leave.EmployeeId, cancellationToken);
        if (applicant != null && applicant.OrganizationId.HasValue)
        {
            DispatchRejectionEmail(applicant, leave, dto.Comments);
        }

        return (true, "Leave request rejected.", 200);
    }

    private static IEnumerable<LeaveRequestSummaryDto> MapToSummaryDtos(IEnumerable<LeaveRequest> items) =>
        items.Select(MapToSummaryDto);

    private static LeaveRequestSummaryDto MapToSummaryDto(LeaveRequest l) => new()
    {
        Id = l.Id,
        EmployeeId = l.EmployeeId,
        EmployeeName = l.Employee?.FullName ?? "N/A",
        EmployeeCode = l.Employee?.EmployeeCode ?? (l.EmployeeId != Guid.Empty ? l.EmployeeId.ToString().Substring(0, 8) : "N/A"),
        Department = l.Employee?.Department?.Name ?? "Unassigned",
        LeaveTypeId = l.LeaveTypeId,
        LeaveTypeName = l.LeaveType?.Name ?? "N/A",
        LeaveType = l.LeaveType != null ? new LeaveTypeSummaryDto
        {
            Id = l.LeaveType.Id,
            Name = l.LeaveType.Name
        } : null,
        HandoverUserId = l.HandoverUserId,
        HandoverUserName = l.HandoverUser?.FullName,
        StartDate = l.StartDate,
        EndDate = l.EndDate,
        NumberOfDays = l.NumberOfDays,
        Reason = l.Reason ?? string.Empty,
        Status = l.Status.ToString(),
        ManagerComments = l.ManagerComments,
        CreatedAt = l.CreatedAt
    };

    private void DispatchNewLeaveRequestEmail(User applicant, int businessDays, CreateLeaveRequestDto dto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var realtimeService = scope.ServiceProvider.GetService<IRealtimeNotificationService>();

                var settings = await dbContext.NotificationSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.OrganizationId == applicant.OrganizationId!.Value);

                if (settings == null || settings.EnableNewLeaveRequestEmails)
                {
                    var recipients = await dbContext.Users
                        .Where(u => u.OrganizationId == applicant.OrganizationId &&
                                    (u.Role == UserRole.HR ||
                                     (applicant.TeamLeadId.HasValue && u.Id == applicant.TeamLeadId.Value) ||
                                     (applicant.DepartmentId.HasValue && u.DepartmentId == applicant.DepartmentId.Value && u.Role == UserRole.TeamLead)))
                        .Select(u => new { u.Id, u.Email })
                        .ToListAsync();

                    string notificationTitle = "New Leave Request";
                    string notificationMsg = $"{applicant.FullName} requested {businessDays} day(s) from {dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}.";

                    string subject = $"New Leave Request - {applicant.FullName}";
                    string body = $@"
                        <h3>New Leave Request Submitted</h3>
                        <p><strong>Employee:</strong> {applicant.FullName} ({applicant.EmployeeCode ?? "N/A"})</p>
                        <p><strong>Working Days:</strong> {businessDays}</p>
                        <p><strong>Dates:</strong> {dto.StartDate:yyyy-MM-dd} to {dto.EndDate:yyyy-MM-dd}</p>
                        <p><strong>Reason:</strong> {dto.Reason}</p>
                        <p>Log in to LeaveFlow to review and process this request.</p>";

                    foreach (var recipient in recipients)
                    {
                        if (!string.IsNullOrEmpty(recipient.Email))
                        {
                            await emailService.SendEmailAsync(recipient.Email, subject, body);
                        }

                        if (realtimeService != null)
                        {
                            await realtimeService.SendNotificationToUserAsync(recipient.Id, notificationTitle, notificationMsg);
                        }
                    }

                    if (dto.HandoverUserId.HasValue)
                    {
                        var handoverUser = await dbContext.Users.FindAsync(dto.HandoverUserId.Value);
                        if (handoverUser != null)
                        {
                            if (!string.IsNullOrEmpty(handoverUser.Email))
                            {
                                string handoverSubject = $"Handover Assignment - {applicant.FullName}";
                                string handoverBody = $@"
                                    <h3>Leave Handover Assignment</h3>
                                    <p>Hi {handoverUser.FullName},</p>
                                    <p><strong>{applicant.FullName}</strong> has designated you as their handover contact for their upcoming leave from <strong>{dto.StartDate:yyyy-MM-dd}</strong> to <strong>{dto.EndDate:yyyy-MM-dd}</strong>.</p>
                                    <p><strong>Reason:</strong> {dto.Reason}</p>";

                                await emailService.SendEmailAsync(handoverUser.Email, handoverSubject, handoverBody);
                            }

                            if (realtimeService != null)
                            {
                                await realtimeService.SendNotificationToUserAsync(handoverUser.Id, "Handover Assignment", $"{applicant.FullName} designated you as their leave handover contact.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new leave request notification email or SignalR event.");
            }
        });
    }

    private void DispatchApprovalEmail(User applicant, LeaveRequest leave, string? comments)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var realtimeService = scope.ServiceProvider.GetService<IRealtimeNotificationService>();

                var settings = await dbContext.NotificationSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.OrganizationId == applicant.OrganizationId!.Value);

                if (settings == null || settings.EnableLeaveStatusUpdateEmails)
                {
                    string subject = "Leave Request Approved - LeaveFlow";
                    string body = $@"
                        <h3>Your Leave Request Has Been Approved!</h3>
                        <p>Hi {applicant.FullName},</p>
                        <p>Your leave request for <strong>{leave.StartDate:yyyy-MM-dd}</strong> to <strong>{leave.EndDate:yyyy-MM-dd}</strong> ({leave.NumberOfDays} working days) has been <strong style='color: green;'>APPROVED</strong>.</p>
                        <p><strong>Comments:</strong> {comments ?? "None"}</p>
                        <p>Your remaining leave balance is <strong>{applicant.LeaveBalance}</strong> days.</p>";

                    await emailService.SendEmailAsync(applicant.Email, subject, body);

                    if (realtimeService != null)
                    {
                        await realtimeService.SendNotificationToUserAsync(
                            applicant.Id,
                            "Leave Request Approved",
                            $"Your leave request from {leave.StartDate:yyyy-MM-dd} to {leave.EndDate:yyyy-MM-dd} was approved."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approval email or SignalR event to employee.");
            }
        });
    }

    private void DispatchRejectionEmail(User applicant, LeaveRequest leave, string? comments)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var realtimeService = scope.ServiceProvider.GetService<IRealtimeNotificationService>();

                var settings = await dbContext.NotificationSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.OrganizationId == applicant.OrganizationId!.Value);

                if (settings == null || settings.EnableLeaveStatusUpdateEmails)
                {
                    string subject = "Leave Request Update - LeaveFlow";
                    string body = $@"
                        <h3>Your Leave Request Status Update</h3>
                        <p>Hi {applicant.FullName},</p>
                        <p>Your leave request for <strong>{leave.StartDate:yyyy-MM-dd}</strong> to <strong>{leave.EndDate:yyyy-MM-dd}</strong> has been <strong style='color: red;'>REJECTED</strong>.</p>
                        <p><strong>Reason / Comments:</strong> {comments ?? "No comments provided."}</p>";

                    await emailService.SendEmailAsync(applicant.Email, subject, body);

                    if (realtimeService != null)
                    {
                        await realtimeService.SendNotificationToUserAsync(
                            applicant.Id,
                            "Leave Request Rejected",
                            $"Your leave request from {leave.StartDate:yyyy-MM-dd} to {leave.EndDate:yyyy-MM-dd} was rejected."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending rejection email or SignalR event to employee.");
            }
        });
    }
}