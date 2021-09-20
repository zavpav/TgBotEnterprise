using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication;
using RabbitMqInfrastructure;

namespace MainBotService.Bot
{
    public interface IMainBot
    {
        void Initialize();
    }

    public class MainBot : IMainBot
    {
        private readonly IRabbitService _rabbitService;

        public MainBot(IRabbitService rabbitService)
        {
            this._rabbitService = rabbitService;
        }

        public void Initialize()
        {
            this._rabbitService.Subscribe<TelegramIncomeMessage>(EnumInfrastructureServicesType.Messaging,
                RabbitMessages.TelegramMessageReceived,
                this.ProcessIncomeTelegramMessage);
        }

        private async Task ProcessIncomeTelegramMessage(TelegramIncomeMessage message, IDictionary<string, string> rabbitmessageheaders)
        {
            if (message.IsEdited)
                return;

            await this._rabbitService.PublishInformation(
                RabbitMessages.TelegramOutgoingMessage,
                new TelegramOutgoingMessage {ChatId = message.ChatId, Message = message.Message, MessageId = message.MessageId});
        }
    }
}