using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication;

namespace MainBotService.MainBotParts
{
    /// <summary> Process income telegram messages </summary>
    public class TelegramProcessor
    {
        public TelegramProcessor()
        {
        }

        public Task<List<TelegramOutgoingMessage>> ProcessIncomeMessage(TelegramIncomeMessage incomeMessage)
        {
            return Task.FromResult(new List<TelegramOutgoingMessage>
            {
                new TelegramOutgoingMessage
                {
                    Message = incomeMessage.BotUserId == GlobalConstants.UndefinedBotUserId 
                        ? "Who are you? " + incomeMessage.MessageText 
                        : "Response " + incomeMessage.MessageText, 
                    ChatId = incomeMessage.ChatId
                }
            });
        }

    }
}