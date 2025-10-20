using AttendanceReportService.Services;

namespace AttendanceReportService.BackgroundJobs
{
    public class HealthMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HealthMonitorService> _logger;

        public HealthMonitorService(
            IServiceScopeFactory scopeFactory,
            ILogger<HealthMonitorService> logger
        )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<DeviceHealthService>();

                    int updated = await service.MarkOfflineDevicesAsync(15);

                    if (updated > 0)
                        _logger.LogInformation(
                            "⚠️ {Count} devices marked offline at {Time}",
                            updated,
                            DateTime.UtcNow
                        );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running health monitor service.");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
