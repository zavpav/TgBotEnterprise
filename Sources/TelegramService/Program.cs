using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;
using System.Text.Json;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMessageCommunication;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Telegram.Bot;
using TelegramService.Database;
using TelegramService.RabbitCommunication;
using TelegramService.Telegram;

namespace TelegramService
{
    public class Program
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
                    ConfigurationServiceExtension.ConfigureServices<DirectRequestProcessor>(configuration, services,
                        EnumInfrastructureServicesType.Messaging);

                    services.AddDbContext<TgServiceDbContext>(optionsBuilder =>
                    {
                        var postgreHost = configuration.GetValue<string?>("POSTGRE_HOST")
                                          ?? throw new NotSupportedException("Postgre host is not initialized");
                        var postgrePort = configuration.GetValue<string?>("POSTGRE_PORT")
                                          ?? throw new NotSupportedException("Postgre port is not initialized");

                        Console.WriteLine($"PostgreConnection: {postgreHost}:{postgrePort}");
                        optionsBuilder.UseNpgsql($"Host={postgreHost};Port={postgrePort};Database=telegram;Username=postgres;Password=123456")
                            .LogTo(Console.WriteLine, LogLevel.Information)
                            //.EnableSensitiveDataLogging()
                            .EnableDetailedErrors();
                    });

                    
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

                    services.AddHostedService<RabbitWorker>();
                    services.AddHostedService<TgPullWorker>();
                });
    }

}
