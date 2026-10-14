using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Infrastructure.BackgroundServices;

public class BirthdayBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BirthdayBackgroundService> _logger;

    public BirthdayBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BirthdayBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Birthday Automation Background Service initialized.");

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBirthdaysAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing automated birthdays.");
            }

            // Run once every 24 hours
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessBirthdaysAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var today = DateTime.UtcNow.Date;

        _logger.LogInformation("Running daily birthday check for date: {Date}", today.ToShortDateString());

        // Fetch users whose DateOfBirth month and day match today
        var celebrants = await context.Users
            .Include(u => u.Department)
            .Where(u => u.DateOfBirth.HasValue &&
                        u.DateOfBirth.Value.Month == today.Month &&
                        u.DateOfBirth.Value.Day == today.Day)
            .ToListAsync(cancellationToken);

        if (!celebrants.Any())
        {
            _logger.LogInformation("No birthdays found for today.");
            return;
        }

        foreach (var celebrant in celebrants)
        {
            try
            {
                _logger.LogInformation("Processing birthday for celebrant: {Name} ({Email})", celebrant.FullName, celebrant.Email);

                // 1. Send Happy Birthday Email to the Celebrant
                string celebrantSubject = "🎉 Happy Birthday from LeaveFlow!";
                string celebrantBody = $"Dear {celebrant.FullName},\n\nWishing you a fantastic and joyous Happy Birthday! To help you celebrate, an automatic leave day has been scheduled for you today.\n\nBest regards,\nLeaveFlow HR Team";

                await emailService.SendEmailAsync(celebrant.Email, celebrantSubject, celebrantBody);

                // 2. Send Notification Email to Department Colleagues
                if (celebrant.DepartmentId.HasValue)
                {
                    var colleagues = await context.Users
                        .Where(u => u.DepartmentId == celebrant.DepartmentId && u.Id != celebrant.Id && u.OrganizationId == celebrant.OrganizationId)
                        .ToListAsync(cancellationToken);

                    if (colleagues.Any())
                    {
                        string colleagueSubject = $"🎂 Today is {celebrant.FullName}'s Birthday!";
                        string colleagueBody = $"Hi Team,\n\nJust a quick reminder that today is {celebrant.FullName}'s birthday ({celebrant.Department?.Name ?? "Department"})! Join us in wishing them a wonderful day.\n\nBest regards,\nLeaveFlow";

                        foreach (var colleague in colleagues)
                        {
                            try
                            {
                                await emailService.SendEmailAsync(colleague.Email, colleagueSubject, colleagueBody);
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "Failed to send birthday notification email to colleague {Email}", colleague.Email);
                            }
                        }
                    }
                }

                // 3. Auto-Schedule Leave Day for the User (if not already booked for today)
                bool alreadyHasLeaveToday = await context.LeaveRequests
                    .AnyAsync(lr => lr.EmployeeId == celebrant.Id &&
                                    lr.Status == LeaveStatus.Approved &&
                                    lr.StartDate <= today &&
                                    lr.EndDate >= today,
                              cancellationToken);

                if (!alreadyHasLeaveToday)
                {
                    var leaveType = await context.LeaveTypes
                        .FirstOrDefaultAsync(lt => lt.OrganizationId == celebrant.OrganizationId &&
                            (lt.Name.Contains("Birthday") || lt.Name.Contains("Casual") || lt.Name.Contains("Annual")),
                            cancellationToken);

                    if (leaveType != null)
                    {
                        var autoLeaveRequest = new LeaveRequest
                        {
                            Id = Guid.NewGuid(),
                            OrganizationId = celebrant.OrganizationId,
                            EmployeeId = celebrant.Id,
                            LeaveTypeId = leaveType.Id,
                            StartDate = today,
                            EndDate = today,
                            NumberOfDays = 1,
                            Reason = "Automatic Birthday Leave",
                            Status = LeaveStatus.Approved,
                            Approved = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        await context.LeaveRequests.AddAsync(autoLeaveRequest, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Successfully auto-scheduled birthday leave for {Name}", celebrant.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete birthday processing for celebrant {Email}", celebrant.Email);
            }
        }
    }
}