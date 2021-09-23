using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommonInfrastructure;
using JenkinsService.RabbitCommunication;
using Serilog;

namespace JenkinsService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigurationServiceExtension.RunApp(CreateHostBuilder(args));
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    ConfigurationServiceExtension.ConfigureServices<DirectRequestProcessor>(configuration, services,
                        EnumInfrastructureServicesType.BuildService);

                    services.AddHostedService<Worker>();
                });
    }
}
