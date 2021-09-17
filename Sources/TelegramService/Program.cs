using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonInfrastructure;
using TelegramService.RabbitCommunication;

namespace TelegramService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigurationServiceExtension.RunApp(CreateHostBuilder(args));
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigurationServiceExtension.ConfigureServices<DirectRequestProcessor>(services,
                        EnumInfrastructureServicesType.Messaging);

                    services.AddHostedService<Worker>();
                });
    }
}
