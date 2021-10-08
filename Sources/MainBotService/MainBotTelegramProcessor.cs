using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;

namespace MainBotService
{
    public partial class MainBotWorker
    {
        /// <summary> Process income telegram messages </summary>
        public class TelegramProcessor
        {
            private readonly MainBotWorker _owner;

            public TelegramProcessor(MainBotWorker owner)
            {
                this._owner = owner;
            }

            public async Task<List<TelegramOutgoingMessage>> ProcessIncomeMessage(TelegramIncomeMessage incomeMessage)
            {
                var user = await this._owner._dbContext.UsersInfo.FirstAsync(x => x.BotUserId == incomeMessage.BotUserId);
                
                // "Inactive user"
                if (!user.IsActive)
                {
                    if (incomeMessage.IsDirectMessage)
                        return new List<TelegramOutgoingMessage>
                        {
                            new TelegramOutgoingMessage
                            {
                                SystemEventId = incomeMessage.SystemEventId,
                                Message = "Who are you? " + incomeMessage.MessageText,
                                ChatId = incomeMessage.ChatId
                            }
                        };
                    else 
                        return new List<TelegramOutgoingMessage>(); // Ignore group messages
                }



                if (incomeMessage.MessageText.ToUpper() == "МОИ ЗАДАЧИ")
                {
                    return new List<TelegramOutgoingMessage>
                    {
                        new TelegramOutgoingMessage
                        {
                            SystemEventId = incomeMessage.SystemEventId,
                            Message = "Ща соберём " + incomeMessage.MessageText,
                            ChatId = incomeMessage.ChatId
                        }
                    };

                }



                return new List<TelegramOutgoingMessage>
                {
                    new TelegramOutgoingMessage
                    {
                        SystemEventId = incomeMessage.SystemEventId,
                        Message = "Unknown command " + incomeMessage.MessageText,
                        ChatId = incomeMessage.ChatId
                    }
                };

            }

        }
    }
}