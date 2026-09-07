using LeaveManagement.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Application.Services;

public class LeaveResetService : ILeaveResetService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<LeaveResetService> _logger;

    public LeaveResetService(IAppDbContext context, ILogger<LeaveResetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, int ProcessedCount)> ResetAnnualLeaveBalancesAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        int currentYear = DateTime.UtcNow.Year;

        var query = _context.Users
            .Include(u => u.Organization)
            .AsQueryable();

        if (organizationId.HasValue)
        {
            query = query.Where(u => u.OrganizationId == organizationId.Value);
        }

        var usersToReset = await query
            .Where(u => !u.LastLeaveResetYear.HasValue || u.LastLeaveResetYear.Value < currentYear)
            .ToListAsync(cancellationToken);

        if (!usersToReset.Any())
        {
            return (true, $"All employee leave balances are already updated for {currentYear}.", 0);
        }

        foreach (var user in usersToReset)
        {
            int defaultDays = user.Organization?.DefaultAnnualLeaveDays ?? 20;
            user.LeaveBalance = defaultDays;
            user.LastLeaveResetYear = currentYear;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Annual leave reset completed. {Count} users updated for year {Year}.", usersToReset.Count, currentYear);

        return (true, $"Successfully reset leave balances for {usersToReset.Count} employee(s) for the year {currentYear}.", usersToReset.Count);
    }
}