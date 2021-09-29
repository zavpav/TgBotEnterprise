using System;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommonInfrastructure;
using MainBotService.Database;
using MainBotService.MainBotParts;
using MainBotService.RabbitCommunication;
using Serilog;

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
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    ConfigurationServiceExtension.ConfigureServices<DirectRequestProcessor>(configuration, services,
                        EnumInfrastructureServicesType.Main);

                    services.ConfigureDatabase<BotServiceDbContext>("main_bot", configuration);


                    var mapperConfig = new MapperConfiguration(mc =>
                    {
                        mc.AddProfile(new MappingProfile());
                    });
                    var mapper = mapperConfig.CreateMapper();
                    services.AddSingleton<IMapper>(mapper);


                    services.AddSingleton<TelegramProcessor>();
                    services.AddHostedService<MainBotWorker>();
                });
    }
}
