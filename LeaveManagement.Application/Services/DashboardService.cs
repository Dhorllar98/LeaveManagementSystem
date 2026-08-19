using LeaveManagement.Application.DTOs.Dashboard;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

namespace LeaveManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ILeaveRequestRepository _leaveRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAppDbContext _context;

    public DashboardService(
        ILeaveRequestRepository leaveRepository,
        IUserRepository userRepository,
        IAppDbContext context)
    {
        _leaveRepository = leaveRepository;
        _userRepository = userRepository;
        _context = context;
    }

    public async Task<DashboardResponseDto?> GetUserDashboardStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return null;

        var userRequests = await _leaveRepository.GetByEmployeeIdAsync(userId, cancellationToken)
                           ?? new List<Domain.Entities.LeaveRequest>();

        return new DashboardResponseDto
        {
            EmployeeName = user.FullName,
            PendingRequestsCount = userRequests.Count(r => r.Status == LeaveStatus.Pending),
            ApprovedLeavesCount = userRequests.Count(r => r.Status == LeaveStatus.Approved),
            RejectedLeavesCount = userRequests.Count(r => r.Status == LeaveStatus.Rejected),
            TotalLeaveDaysRemaining = user.LeaveBalance
        };
    }

    public async Task<AdminDashboardResponseDto?> GetAdminDashboardStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var adminUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (adminUser?.OrganizationId == null) return null;

        var orgId = adminUser.OrganizationId.Value;

        var orgLeaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.OrganizationId == orgId)
            .ToListAsync(cancellationToken);

        var orgUsers = await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == orgId)
            .ToListAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;

        return new AdminDashboardResponseDto
        {
            TotalEmployees = orgUsers.Count,
            TotalRequestsCount = orgLeaves.Count,
            PendingApprovalsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Pending),
            ApprovedRequestsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Approved),
            RejectedRequestsCount = orgLeaves.Count(l => l.Status == LeaveStatus.Rejected),
            EmployeesCurrentlyOnLeave = orgLeaves.Count(l => l.Status == LeaveStatus.Approved
                                                         && l.StartDate.Date <= today
                                                         && l.EndDate.Date >= today)
        };
    }
}