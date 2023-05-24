using System.Threading.Tasks;
using IDGenerator.Config;
using IDGenerator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;

namespace IDGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                 .ConfigureHostConfiguration(configHost =>
                 {
                     configHost.AddJsonFile("hostsettings.json", optional: false);
                 })
                 .ConfigureAppConfiguration((hostContext, configApp) =>
                 {
                     configApp.AddJsonFile("appsettings.json", optional: true);
                     configApp.AddJsonFile(
                         $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                         optional: true);
                     configApp.AddCommandLine(args);
                 })
                 .ConfigureServices((hostContext, services) =>
                 {
                     //Inject options
                     services.Configure<APIOptions>(hostContext.Configuration.GetSection("APIOptions"));
                     services.Configure<AppOptions>(hostContext.Configuration.GetSection("AppOptions"));

                     //Inject Services
                     services.AddTransient<IAPIService, APIService>();

                     //Inject Host Service
                     services.AddHostedService<HostedService>();
                 })
                 .ConfigureLogging((hostContext, configLogging) =>
                 {
                     configLogging.AddNLog();
                 })
                 .UseConsoleLifetime()
                 .Build();

            await host.RunAsync();
        }
    }
}