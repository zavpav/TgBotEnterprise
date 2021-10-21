using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.BugTracker;
using RabbitMqInfrastructure;
using Serilog;

namespace MainBotService.RabbitCommunication
{
    public partial class MainBotService
    {
        /// <summary> Process income telegram messages </summary>
        public class TelegramProcessor
        {
            private readonly MainBotService _owner;
            private readonly ILogger _logger;

            /// <summary> Cache for response waters </summary>
            private readonly System.Runtime.Caching.MemoryCache _waitResponseCache = new System.Runtime.Caching.MemoryCache("responseWaiter");

            public TelegramProcessor(MainBotService owner, ILogger logger)
            {
                this._owner = owner;
                this._logger = logger;
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



                if (incomeMessage.MessageText.ToUpper() == "МОИ ЗАДАЧИ" || incomeMessage.MessageText.ToUpper() == "MY TASKS")
                {
                    this._logger.Information(incomeMessage, "Processing 'MY TASKS' message");
                    var requestMessage = new BugTrackerTasksRequestMessage(incomeMessage.SystemEventId)
                    {
                        FilterUserBotId = incomeMessage.BotUserId
                    }; 

                    var responseMessage = await this._owner._rabbitService.DirectRequestTo<BugTrackerTasksResponseMessage>(
                        EnumInfrastructureServicesType.BugTracker,
                        RabbitMessages.BugTrackerRequestIssues,
                        requestMessage
                        );

                    return new List<TelegramOutgoingMessage>
                    {
                        new TelegramOutgoingMessage
                        {
                            SystemEventId = incomeMessage.SystemEventId,
                            Message = "Ща соберём " + JsonSerializer.Serialize(responseMessage),
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