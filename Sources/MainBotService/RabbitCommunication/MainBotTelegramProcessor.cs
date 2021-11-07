using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonInfrastructure;
using MainBotService.RabbitCommunication.TelegramDialoges;
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
            private readonly Lazy<IEnumerable<ITelegramConversation>> _telegraConversations;
            private readonly ILogger _logger;

            /// <summary> Cache for response waters </summary>
            private readonly System.Runtime.Caching.MemoryCache _waitResponseCache = new System.Runtime.Caching.MemoryCache("responseWaiter");

            public TelegramProcessor(MainBotService owner,
                Lazy<IEnumerable<ITelegramConversation>> telegraConversations, 
                ILogger logger)
            {
                this._owner = owner;
                this._telegraConversations = telegraConversations;
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

                //find uncomplited conversation
                //..

                //find conversation by first message
                var conversations = this._telegraConversations.Value.ToList();
                foreach (var conversation in conversations)
                {
                    if (await conversation.IsStartingMessage(incomeMessage.MessageText))
                    {
                        var nextStepName = await conversation.NextConversationStep("first", incomeMessage.MessageText);
                        if (nextStepName != null)
                        {
                            //..
                        }
                    }
                }

                // others
                if (incomeMessage.MessageText.ToUpper() == "МОИ ЗАДАЧИ" || incomeMessage.MessageText.ToUpper() == "MY TASKS")
                {
                    this._logger.Information(incomeMessage, "Processing 'MY TASKS' message");
                    var requestMessage = new BugTrackerTasksRequestMessage(incomeMessage.SystemEventId)
                    {
                        FilterUserBotId = incomeMessage.BotUserId,
                        FilterStatus = new []{"Черновик", "В работе"}
                    }; 

                    var responseMessage = await this._owner._rabbitService.DirectRequestTo<BugTrackerTasksRequestMessage, BugTrackerTasksResponseMessage>(
                        EnumInfrastructureServicesType.BugTracker,
                        RabbitMessages.BugTrackerRequestIssues,
                        requestMessage
                        );

                    var issues = responseMessage.Issues.Where(x => x.Status == "Черновик").ToList();

                    var sb = new StringBuilder(200);
                    if (issues.Count > 10)
                    {
                        sb.Append("Слишком много задач\n");
                        sb.Append(issues.Count);
                    }
                    else
                    {
                        sb.Append("Задачи\n");
                        foreach (var issue in issues)
                        {
                            sb.AppendFormat("#{0} {1}\n", issue.Num, issue.Subject);
                            sb.AppendFormat("   Статус: {0}\n   Исполнитель: {1}\n", issue.Status, issue.AssignOn);
                            sb.Append("\n");
                        }
                    }

                    return new List<TelegramOutgoingMessage>
                    {
                        new TelegramOutgoingMessage
                        {
                            SystemEventId = incomeMessage.SystemEventId,
                            Message = sb.ToString(),
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