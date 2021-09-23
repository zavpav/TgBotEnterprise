using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonInfrastructure;
using MainBotService.MainBotParts;
using RabbitMessageCommunication;
using RabbitMqInfrastructure;

namespace MainBotService
{
    public class MainBotWorker : BackgroundService
    {
        private readonly ILogger<MainBotWorker> _logger;
        private readonly IRabbitService _rabbitService;
        private readonly TelegramProcessor _telegramProcessor;

        public MainBotWorker(ILogger<MainBotWorker> logger, 
            IRabbitService rabbitService,
            TelegramProcessor telegramProcessor)
        {
            this._logger = logger;
            this._rabbitService = rabbitService;
            _telegramProcessor = telegramProcessor;
        }

        #region Initialization

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.Initialize();

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("MainBotWorker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private void Initialize()
        {
            this._rabbitService.Subscribe<TelegramIncomeMessage>(EnumInfrastructureServicesType.Messaging,
                RabbitMessages.TelegramMessageReceived,
                this.ProcessIncomeTelegramMessage);
        }

        #endregion

        private async Task ProcessIncomeTelegramMessage(TelegramIncomeMessage incomeMessage, IDictionary<string, string> rabbitMessageHeaders)
        {
            if (incomeMessage.IsEdited)
                return;

            var responseMessages = await this._telegramProcessor.ProcessIncomeMessage(incomeMessage).ConfigureAwait(false);

            foreach (var outgoingMessage in responseMessages)
            {
                await this._rabbitService.PublishInformation(
                    RabbitMessages.TelegramOutgoingMessage,
                    outgoingMessage);
            }
        }

    }
}
