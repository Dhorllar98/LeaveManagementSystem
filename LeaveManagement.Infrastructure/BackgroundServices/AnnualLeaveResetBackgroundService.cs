using LeaveManagement.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LeaveManagement.Infrastructure.BackgroundServices;

public class AnnualLeaveResetBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnnualLeaveResetBackgroundService> _logger;

    public AnnualLeaveResetBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AnnualLeaveResetBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Annual Leave Reset Background Service initialized.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var resetService = scope.ServiceProvider.GetRequiredService<ILeaveResetService>();

                var result = await resetService.ResetAnnualLeaveBalancesAsync(null, stoppingToken);

                if (result.ProcessedCount > 0)
                {
                    _logger.LogInformation("Automated Background Reset Result: {Message}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during execution of Annual Leave Reset Background Service.");
            }

            // Runs once every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}