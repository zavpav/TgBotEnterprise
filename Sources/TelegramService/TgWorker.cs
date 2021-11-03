using System;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using RabbitMqInfrastructure;
using Serilog;
using TelegramService.Telegram;

namespace TelegramService
{
    public class TgWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ITelegramWrap _telegramWrap;
        private readonly IRabbitProcessor _rabbitProcessor;

        public TgWorker(ILogger logger, IRabbitProcessor rabbitProcessor, ITelegramWrap telegramWrap)
        {
            this._logger = logger;
            this._rabbitProcessor = rabbitProcessor;
            this._telegramWrap = telegramWrap;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await this._telegramWrap.Pull();
                    //                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception e)
                {
                    this._logger.Information(e, "Exception TelegramService");
                }
            }
        }
    }
}
