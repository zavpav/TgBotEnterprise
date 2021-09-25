using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonInfrastructure;
using MainBotService.MainBotParts;
using RabbitMessageCommunication;
using RabbitMqInfrastructure;
using Serilog;

namespace MainBotService
{
    public class MainBotWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IRabbitService _rabbitService;
        private readonly TelegramProcessor _telegramProcessor;

        public MainBotWorker(ILogger logger, 
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
            {
                this._logger.Information(incomeMessage, "Message edited. Now ignoring. {@incomeMessage} ", incomeMessage);
                return;
            }

            this._logger.Information(incomeMessage, "Processing message. {@incomeMessage} ", incomeMessage);
            var responseMessages = await this._telegramProcessor.ProcessIncomeMessage(incomeMessage).ConfigureAwait(false);

            if (responseMessages.Count == 0)
            {
                this._logger.Information(incomeMessage, "Nothing to send");
                return;
            }

            foreach (var outgoingMessage in responseMessages)
            {
                this._logger.Information(outgoingMessage, "Send message through telegram. {@outgoingMessage}", outgoingMessage);
                await this._rabbitService.PublishInformation(
                    RabbitMessages.TelegramOutgoingMessage,
                    outgoingMessage);
            }
        }

    }
}
