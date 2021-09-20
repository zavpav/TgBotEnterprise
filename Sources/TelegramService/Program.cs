using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommonInfrastructure;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
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
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigurationServiceExtension.ConfigureServices<DirectRequestProcessor>(services,
                        EnumInfrastructureServicesType.Messaging);

                    var variables = Environment.GetEnvironmentVariables();
                    var tgKey = (string?) variables["TelegramKey"];

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
