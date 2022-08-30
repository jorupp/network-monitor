using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetworkMonitor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {

                })
                .ConfigureServices((builder, services) =>
                {
                    services.AddOptions<Settings>().Bind(builder.Configuration.GetSection(Settings.Section));
                    services.AddHostedService<NetworkMonitorService>();
                })
                .Build();

            host.Run();
        }
    }
}