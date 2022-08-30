using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetworkMonitor
{
    public class NetworkMonitorService : BackgroundService
    {
        public NetworkMonitorService(ILogger<NetworkMonitorService> logger, IOptionsMonitor<Settings> settings)
        {
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public ILogger<NetworkMonitorService> Logger { get; }
        protected IOptionsMonitor<Settings> Settings { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = this.Settings.CurrentValue;

            this.Logger.LogInformation($"{settings.Http?.Count}");
            this.Logger.LogInformation($"{settings.Ping?.Count}");

            var startTime = DateTime.UtcNow;
            var nextRunTime = startTime
                .Subtract(TimeSpan.FromMilliseconds(startTime.TimeOfDay.TotalMilliseconds % settings.Interval.TotalMilliseconds));

            this.Logger.LogInformation($"Start time: {startTime}");
            this.Logger.LogInformation($"Next run time: {nextRunTime}");

            while (!stoppingToken.IsCancellationRequested)
            {
                // if necessary, wait for the appropriate time to do the next run
                var toWait = nextRunTime.Subtract(DateTime.UtcNow);
                if (toWait > settings.TimeAllowedEarly)
                {
                    this.Logger.LogInformation($"Waiting {toWait}.");
                    await Task.Delay(toWait);
                }
                else if (toWait.TotalMilliseconds > 0)
                {
                    this.Logger.LogInformation($"Allowing to run {toWait} early.");
                }
                else
                {
                    this.Logger.LogInformation($"Running {-toWait} late.");
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                // calculate next time to run
                var now = DateTime.UtcNow;
                nextRunTime = nextRunTime.Add(settings.Interval);
                if (nextRunTime < now)
                {
                    this.Logger.LogWarning($"Next scheduled time ({nextRunTime}) is in the past - skipping intervals to catch up.");
                    while (nextRunTime < now)
                    {
                        nextRunTime = nextRunTime.Add(settings.Interval);
                    }
                    this.Logger.LogWarning($"New next scheduled time is {nextRunTime}.");
                }

                // launch the tests (but don't wait for them to complete)

            }
        }
    }
}
