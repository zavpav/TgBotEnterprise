using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using CommonInfrastructure;
using RedmineService.RabbitCommunication;
using Serilog;
using Serilog.Events;

namespace RedmineService
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
                        EnumInfrastructureServicesType.BugTracker);

                    services.AddHostedService<Worker>();
                });
    }
}
