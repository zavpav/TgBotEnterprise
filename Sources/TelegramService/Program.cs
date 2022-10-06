using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;
using System.Text.Json;
using AutoMapper;
using CommonInfrastructure;
using Microsoft.Extensions.Configuration;
using Serilog;
using Telegram.Bot;
using TelegramService.Database;
using TelegramService.RabbitCommunication;
using TelegramService.Telegram;

namespace TelegramService
{
    public static class Program
    {

        public static void Main(string[] args)
        {
            ConfigurationServiceExtension.RunApp(CreateHostBuilder(args), host =>
                {
                    host.Services.GetRequiredService<ITelegramWrap>().Initialize();
                });
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    ConfigurationServiceExtension.ConfigureServices<RabbitProcessor>(configuration, services,
                        EnumInfrastructureServicesType.Messaging);

                    services.ConfigureDatabase<TgServiceDbContext>("telegram", configuration);

                    var mapperConfig = new MapperConfiguration(mc =>
                    {
                        mc.AddProfile(new MappingProfile());
                    });
                    var mapper = mapperConfig.CreateMapper();
                    services.AddSingleton<IMapper>(mapper);
                    
                    var tgKey = configuration.GetValue<string?>("TELEGRAM_KEY");

                    try
                    {
                        var jsonString = new System.IO.StreamReader("Secretic/TgConfiguration.json", Encoding.UTF8).ReadToEnd();
                        var tgInfo = JsonSerializer.Deserialize<TelegramInformation>(jsonString);
                        tgKey = tgInfo?.TelegramKey;
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                    }

                    if (tgKey == null)
                        throw new NotSupportedException("TelegramKey undefined");

                    
                    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(tgKey));
                    services.AddSingleton<ITelegramWrap, TelegramWrap>();

                    services.AddHostedService<TgWorker>();
                });
    }

}
