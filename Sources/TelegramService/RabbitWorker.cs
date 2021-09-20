using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonInfrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMessageCommunication;
using RabbitMqInfrastructure;
using TelegramService.Telegram;

namespace TelegramService
{
    public class RabbitWorker : BackgroundService
    {
        private readonly ILogger<TgPullWorker> _logger;
        private readonly ITelegramWrap _telegramWrap;
        private readonly IRabbitService _rabbitService;

        public RabbitWorker(ILogger<TgPullWorker> logger, IRabbitService rabbitService, ITelegramWrap telegramWrap)
        {
            this._logger = logger;
            this._rabbitService = rabbitService;
            this._telegramWrap = telegramWrap;
        }

        private Task OutgoingMessageProcess(TelegramOutgoingMessage messageData, IDictionary<string, string> rabbitMessageheaders)
        {
            return this._telegramWrap.SendMessage(messageData);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._rabbitService.Subscribe<TelegramOutgoingMessage>(EnumInfrastructureServicesType.Main,
                RabbitMessages.TelegramOutgoingMessage,
                this.OutgoingMessageProcess);


            while (!stoppingToken.IsCancellationRequested)
            {
                await this._telegramWrap.Pull();
                //                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

    }
}