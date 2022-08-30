using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetworkMonitor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((builder, services) =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.AddOptions<Settings>().Bind(builder.Configuration.GetSection(Settings.Section));
                    services.AddHostedService<NetworkMonitorService>();
                })
                .Build();

            host.Run();
        }
    }
}