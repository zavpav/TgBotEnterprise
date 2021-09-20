using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMessageCommunication;
using RabbitMqInfrastructure;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace TelegramService.Telegram
{
    public interface ITelegramWrap
    {
        Task Pull();
        Task Initialize();
        Task SendMessage(TelegramOutgoingMessage messageData);
    }

    public class TelegramWrap : ITelegramWrap
    {
        private readonly ITelegramBotClient _telegramBot;
        private readonly IRabbitService _rabbitService;

        private int _tgMessageOffset;

        public TelegramWrap(ITelegramBotClient telegramBot, IRabbitService rabbitService)
        {
            this._telegramBot = telegramBot;
            _rabbitService = rabbitService;

//            this._telegramBot.OnApiResponseReceived += this.OnApiResponseReceived;
        }

        //private async ValueTask OnApiResponseReceived(ITelegramBotClient botclient, 
        //            ApiResponseEventArgs args, 
        //            CancellationToken cancellationtoken)
        //{
        //    await Task.Delay(10);
        //}

        public async Task Pull()
        {
            var messages = await this._telegramBot.GetUpdatesAsync(offset: this._tgMessageOffset);

            if (messages.Length != 0)
            {
                foreach (var msg in messages)
                {
                    var messageData = new TelegramIncomeMessage
                    {
                        UpdateId = msg.Id,
                    };
                    var isProcessed = false;

                    if (msg.Message != null)
                    {
                        isProcessed = true;
                        messageData.IsEdited = false;
                        messageData.ChatId = msg.Message.Chat.Id;
                        messageData.Message = msg.Message.Text;
                        messageData.MessageId = msg.Message.MessageId;
                        messageData.TelegramUserId = msg.Message.From.Id;
                    }
                    else if (msg.EditedMessage != null)
                    {
                        isProcessed = true;
                        messageData.IsEdited = true;
                        messageData.ChatId = msg.EditedMessage.Chat.Id;
                        messageData.Message = msg.EditedMessage.Text;
                        messageData.MessageId = msg.EditedMessage.MessageId;
                        messageData.TelegramUserId = msg.EditedMessage.From.Id;
                    }

                    if (isProcessed)
                        await this._rabbitService.PublishInformation(RabbitMessages.TelegramMessageReceived, messageData);
                }

                this._tgMessageOffset = messages.Max(x => x.Id) + 1;
            }
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public async Task SendMessage(TelegramOutgoingMessage messageData)
        {
            var msg = await this._telegramBot.SendTextMessageAsync(messageData.ChatId, messageData.Message, replyToMessageId: messageData.MessageId ?? 0);
            //msg.MessageId
        }
    }
}