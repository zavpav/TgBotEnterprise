using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonInfrastructure;
using MainBotService.MainBotParts;
using MainBotService.RabbitCommunication;
using RabbitMqInfrastructure;

namespace MainBotService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigurationServiceExtension.RunApp(
                CreateHostBuilder(args)
                //, h => h.Services.GetRequiredService<IMainBot>().Initialize()
                );
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    ConfigurationServiceExtension.ConfigureServices<DirectRequestProcessor>(configuration, services,
                        EnumInfrastructureServicesType.Main);

                    services.AddSingleton<TelegramProcessor>();
                    services.AddHostedService<MainBotWorker>();
                });
    }
}
