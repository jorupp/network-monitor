using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

namespace NetworkMonitor
{
    public class NetworkMonitorService : BackgroundService
    {
        public NetworkMonitorService(TelemetryClient telemetryClient, ILogger<NetworkMonitorService> logger, IOptionsMonitor<Settings> settings)
        {
            this.TelemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public TelemetryClient TelemetryClient { get; }
        public ILogger<NetworkMonitorService> Logger { get; }
        protected IOptionsMonitor<Settings> Settings { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = this.Settings.CurrentValue;

            var startTime = DateTime.UtcNow;
            var nextRunTime = startTime
                .Subtract(TimeSpan.FromMilliseconds(startTime.TimeOfDay.TotalMilliseconds % settings.Interval.TotalMilliseconds));

            this.Logger.LogInformation($"Start time: {startTime}");
            this.Logger.LogInformation($"Next run time: {nextRunTime}");

            while (!stoppingToken.IsCancellationRequested)
            {
                var targetTime = nextRunTime;
                // if necessary, wait for the appropriate time to do the next run
                var toWait = nextRunTime.Subtract(DateTime.UtcNow);
                if (toWait > settings.TimeAllowedEarly)
                {
                    this.Logger.LogDebug($"Waiting {toWait}.");
                    await Task.Delay(toWait);
                }
                else if (toWait.TotalMilliseconds > 0)
                {
                    this.Logger.LogDebug($"Allowing to run {toWait} early.");
                }
                else
                {
                    this.Logger.LogDebug($"Running {-toWait} late.");
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
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                foreach (var http in settings.Http ?? new Dictionary<string, string>())
                {
                    var url = http.Value;
                    DoTest(settings, targetTime, nameof(http), http.Key, () => TestHttp(url));
                }
                foreach (var ping in settings.Ping ?? new Dictionary<string, string>())
                {
                    var address = ping.Value;
                    DoTest(settings, targetTime, nameof(ping), ping.Key, () => TestPing(address));
                }
                foreach (var dns in settings.Dns ?? new Dictionary<string, string>())
                {
                    var address = dns.Value;
                    DoTest(settings, targetTime, nameof(dns), dns.Key, () => TestPing(address));
                }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        protected async Task DoTest(Settings settings, DateTime targetTime, string testType, string testName, Func<Task> test)
        {
            var sw = Stopwatch.StartNew();
            var success = false;
            try
            {
                await test();
                success = true;
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, $"Unable to run test {testType} {testName}");
            }

            var duration = sw.Elapsed;
            if (duration > settings.WarningThreshold)
            {
                Logger.LogWarning($"{testType} {testName} Exceeded threshold: {duration}");

            }

            TelemetryClient.TrackEvent(settings.EventName, new Dictionary<string, string>
            {
                {  nameof(settings.MachineName), settings.MachineName },
                {  nameof(testName), testName },
                {  nameof(testType), testType },
                {  nameof(targetTime), targetTime.ToString("O") },
            }, new Dictionary<string, double>
            {
                { "duration", duration.TotalMilliseconds },
                { "success", success ? 1 : 0 },
            });
        }

        protected async Task TestHttp(string url)
        {
            // we do _not_ want DNS-cache and socket reuse, so we're specifically creating a new instance each time
            using(var client = new HttpClient())
            {
                var res = await client.GetAsync(url);
                res.EnsureSuccessStatusCode();
                var s = res.Content.ReadAsStream();
                byte[] buffer = new byte[4096];
                while(await s.ReadAsync(buffer) > 0)
                {
                }
            }
        }

        protected async Task TestPing(string address)
        {
            var ping = new Ping();
            ping.Send(address);
            await ping.SendPingAsync(address);
        }

        protected async Task TestDns(string address)
        {
            await Dns.GetHostAddressesAsync(address);
        }
    }
}
