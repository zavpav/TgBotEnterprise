using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.BugTracker;
using RabbitMqInfrastructure;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramService.Database;

namespace TelegramService.Telegram
{
    public interface ITelegramWrap
    {
        Task Pull();
        Task Initialize();
        Task SendMessage(TelegramOutgoingMessage messageData);
        void ClearUserCache();

        /// <summary> Send issues to user </summary>
        /// <remarks>
        /// Formatting must be in telegram service because
        /// we might have a lot of issues (and we have to split this information for several messages)
        /// we have a special formatting for issues
        /// </remarks>
        Task SendIssuesMessage(TelegramOutgoingIssuesMessage message);

        Task SendIssueChangedMessage(TelegramOutgoingIssuesChangedMessage message);
    }

    public class TelegramWrap : ITelegramWrap
    {
        private readonly ILogger _logger;
        private readonly TgServiceDbContext _dbContext;
        private readonly ITelegramBotClient _telegramBot;
        private readonly IRabbitService _rabbitService;
        private readonly IGlobalEventIdGenerator _globalEventIdGenerator;

        private readonly AsyncAwaitLock _lock  = new AsyncAwaitLock();

        private int _tgMessageOffset;

        public TelegramWrap(ILogger logger,
            ITelegramBotClient telegramBot, 
            IRabbitService rabbitService,
            IGlobalEventIdGenerator globalEventIdGenerator,
            TgServiceDbContext dbContext)
        {
            this._logger = logger;
            this._dbContext = dbContext;
            this._telegramBot = telegramBot;
            this._rabbitService = rabbitService;
            this._globalEventIdGenerator = globalEventIdGenerator;
        }

        public async Task Pull()
        {
            var messages = await this._telegramBot.GetUpdatesAsync(offset: this._tgMessageOffset);

            if (messages.Length != 0)
            {
                foreach (var msg in messages)
                {
                    var eventId = this._globalEventIdGenerator.GetNextEventId();
                    var tgMsg = msg.Message ?? msg.EditedMessage;
                    if (tgMsg != null)
                    {
                        var cacheInfo = await this.TryUpdateTelegramUserInformation(tgMsg, eventId);

                        var messageData = new TelegramIncomeMessage
                        {
                            UpdateId = msg.Id,
                            SystemEventId = eventId,
                            ChatId = tgMsg.Chat.Id,
                            IsDirectMessage = tgMsg.Chat.Type == ChatType.Private,
                            MessageText = tgMsg.Text,
                            MessageId = tgMsg.MessageId,
                            TelegramUserId = tgMsg.From.Id,
                            BotUserId = cacheInfo.BotUserId,
                        };

                        if (msg.EditedMessage != null)
                            messageData.IsEdited = true;

                        this._logger
                            .ForContext("TelegramUser", 
                                new { 
                                    UserName = tgMsg.From.Username, 
                                    LastName = tgMsg.From.LastName,
                                    FirstName = tgMsg.From.FirstName
                                    },
                                true
                            )
                            .Information(messageData, "Process telegram message {@incomeMessage}", messageData);

                        await this._rabbitService.PublishInformation(RabbitMessages.TelegramMessageReceived, messageData);
                    }
                }

                this._tgMessageOffset = messages.Max(x => x.Id) + 1;
            }
        }

        #region UserCache

        private ConcurrentDictionary<long, UserCache> _usersCache = new ConcurrentDictionary<long, UserCache>();

        public void ClearUserCache()
        {
            this._usersCache = new ConcurrentDictionary<long, UserCache>();
        }

        /// <summary> Try to update telegram information. Add information if it doesn't exist. </summary>
        private async ValueTask<UserCache> TryUpdateTelegramUserInformation(Message tgMsg, string eventId)
        {
            var currUsrInfo = this.CreateTemporaryDtoUserInfoFromTgMessage(tgMsg, eventId);
            if (!this._usersCache.TryGetValue(tgMsg.From.Id, out var usr))
            {
                usr = await this.UpdateDbUser(currUsrInfo, eventId);
            }
            else
            {
                if (currUsrInfo.DefaultChatId != null && usr.DefaultChatId != currUsrInfo.DefaultChatId)
                {

                    currUsrInfo.BotUserId = usr.BotUserId;
                    
                    usr = await this.UpdateDbUser(currUsrInfo, eventId);
                }
            }
            if (!this._usersCache.TryGetValue(tgMsg.From.Id, out usr))
                throw new NotSupportedException("UserCache doesn't have info");

            return usr;
        }

        /// <summary> Update information </summary>
        private async ValueTask<UserCache> UpdateDbUser(DbeUserInfo currUsrInfo, string eventId)
        {
            using var lc = await this._lock.Lock(new TimeSpan(0, 1, 0));

            var usrInfo = await this._dbContext.UsersInfo.FirstOrDefaultAsync(x => x.TelegramUserId == currUsrInfo.TelegramUserId);
            if (usrInfo == null)
            {
                // Add new telegram user
                await this._dbContext.UsersInfo.AddAsync(currUsrInfo);
                await this._dbContext.SaveChangesAsync();

                await this._rabbitService.PublishInformation(RabbitMessages.TelegramPublishNewUserFromTelegram,
                    new TelegramPublishNewUserFromTelegram
                    {
                        SystemEventId = eventId,
                        BotUserId = currUsrInfo.BotUserId,
                        WhoIsThis = currUsrInfo.WhoIsThis
                    });
            }
            else
            {
                if (usrInfo.DefaultChatId != currUsrInfo.DefaultChatId || currUsrInfo.WhoIsThis != usrInfo.WhoIsThis)
                {
                    // Update User Info
                    usrInfo.DefaultChatId = currUsrInfo.DefaultChatId ?? usrInfo.DefaultChatId;
                    usrInfo.WhoIsThis = currUsrInfo.WhoIsThis;
                    usrInfo.BotUserId = currUsrInfo.BotUserId;

                    await this._dbContext.SaveChangesAsync();
                }
            }

            usrInfo = await this._dbContext.UsersInfo.FirstAsync(x => x.TelegramUserId == currUsrInfo.TelegramUserId);

            var userCache = new UserCache(usrInfo.TelegramUserId, usrInfo.DefaultChatId, usrInfo.BotUserId);
            return this._usersCache.AddOrUpdate(usrInfo.TelegramUserId, userCache, (id, exst) => userCache);
        }

        /// <summary> Create tg user information from telegram message  </summary>
        private DbeUserInfo CreateTemporaryDtoUserInfoFromTgMessage(Message tgMsg, string eventId)
        {
            return new DbeUserInfo
            {
                DefaultChatId = tgMsg.Chat.Type != ChatType.Private ? null : (long?) tgMsg.Chat.Id,
                BotUserId = "TG_TMP:" + tgMsg.From.Id,
                IsActive = false,
                TelegramUserId = tgMsg.From.Id,
                WhoIsThis = $"Undefined user:  UserName: {tgMsg.From.Username} LastName: {tgMsg.From.LastName} FirstName: {tgMsg.From.FirstName}"
            };
        }

        private struct UserCache
        {
            public UserCache(long telegramUserId, long? defaultChatId, string botUserId)
            {
                this.TelegramUserId = telegramUserId;
                this.DefaultChatId = defaultChatId;
                this.BotUserId = botUserId;
            }

            public long TelegramUserId { get; }
            public long? DefaultChatId { get; }
            public string BotUserId { get; }

            public bool Equals(UserCache other)
            {
                return TelegramUserId == other.TelegramUserId && DefaultChatId == other.DefaultChatId && BotUserId == other.BotUserId;
            }

            public override bool Equals(object? obj)
            {
                return obj is UserCache other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(TelegramUserId, DefaultChatId, BotUserId);
            }
        }

        #endregion



        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        private async Task<long?> DefineChatId(string? userBotId)
        {
            if (userBotId == null)
            {
                this._logger
                    .Error("Error processing output message. ChatId == null && BotUserId == null");
                return null;
            }

            var usrInfo = await this._dbContext.UsersInfo.AsNoTracking().SingleOrDefaultAsync(x => x.BotUserId == userBotId);
            if (usrInfo == null)
            {
                this._logger
                    .Error("Error processing output message. User '{userBotId}' not found.", userBotId);
                return null;
            }

            if (usrInfo.DefaultChatId == null)
            {
                this._logger
                    .Error("Error processing output message. ChatId not found. User: {userBotId}", userBotId);
                return null;
            }

            return usrInfo.DefaultChatId;
        }

        public async Task SendMessage(TelegramOutgoingMessage messageData)
        {
            var chatId = messageData.ChatId;
            if (chatId == null)
                chatId = await this.DefineChatId(messageData.BotUserId);

            if (chatId == null)
            {
                this._logger
                    .ForContext("message", messageData, true)
                    .Error("Error processing output message. ChatId not found.");
                return;
            }

            this._logger
                .ForContext("outgingMessage", messageData, true)
                .Information(messageData, "Sending message to user");
            try
            {
                var msg = await this._telegramBot.SendTextMessageAsync(chatId.Value, messageData.Message, replyToMessageId: messageData.MessageId ?? 0);
            }
            catch (Exception e)
            {
                this._logger
                    .ForContext("outgingMessage", messageData, true)
                    .Error(messageData, e, "Error sending message to user");
            }
            //msg.MessageId
        }

        public async Task SendIssueChangedMessage(TelegramOutgoingIssuesChangedMessage message)
        {
            await Task.Yield();

            var chatId = await this.DefineChatId(message.BotUserId);
            if (chatId == null)
            {
                this._logger
                    .ForContext("message", message, true)
                    .Error("Error processing output message. ChatId not found.");
                return;
            }

            var htmlString = $"<b>{message.HeaderText.EscapeHtml()}</b>\n\n<a href=\"{message.IssueHttpFullPrefix}{message.IssueNum}\">#{message.IssueNum}</a>  {message.BodyText.EscapeHtml()}";
            await this._telegramBot.SendTextMessageAsync(chatId, htmlString, ParseMode.Html);
        }

        public async Task SendIssuesMessage(TelegramOutgoingIssuesMessage message)
        {
            await Task.Yield();

            if (message.ChatId == null)
            {
                this._logger
                    .ForContext("message", message, true)
                    .Error("NotImplementing yet. ChatId=null. Need to fing defaultChatId in database and send it to defaultChat");
                return;
            }

            var projects = message.Issues
                .GroupBy(x => x.ProjectSysName);

            foreach (var issuesByProject in projects)
            {
                foreach (var issuesByVersion in issuesByProject.GroupBy(x => x.Version))
                {
                    var htmlString = this.FormatHtmlSingleVersionMessage(issuesByProject.Key, 
                        issuesByVersion.Key,
                        message.IssueHttpFullPrefix,
                        issuesByVersion.ToList());

                    await this._telegramBot.SendTextMessageAsync(message.ChatId, htmlString, ParseMode.Html);
                }
            }

        }

        /// <summary> Format issues by proect and version </summary>
        /// <param name="projectSysName">Sys name of project</param>
        /// <param name="version">Version name</param>
        /// <param name="httpIssuePrefix">Web address for open issue</param>
        /// <param name="issues">List of issues</param>
        /// <returns>Formatted string for message as html-string</returns>
        private string FormatHtmlSingleVersionMessage(string projectSysName, 
            string version, 
            string httpIssuePrefix,
            List<BugTrackerIssue> issues)
        {
            var sb = new StringBuilder();
            sb.Append($"<b>Версия по проекту {issues.First().RedmineProjectName.EscapeHtml()}. Версия {version.EscapeHtml()}.</b>\n");
            sb.Append("\n");

            
            foreach (var issuesByStatus in issues
                .GroupBy(x => x.RedmineStatus)
                .Distinct()
                .OrderBy(x =>
                    x.Key.ToLower() == "готов к работе" ? 1 :
                    x.Key.ToLower() == "в работе" ? 3 :
                    x.Key.ToLower() == "переоткрыт" ? 2 :
                    x.Key.ToLower() == "на тестировании" ? 4 :
                    x.Key.ToLower() == "решен" ? 5 : 10
                )
            )
            {
                sb.Append($"\nСтатус: <b>{issuesByStatus.Key.EscapeHtml()}</b>\n");
                foreach (var issue in issuesByStatus)
                {
                    sb.Append(this.DefineRoleIconByUser(issue.UserBotIdAssignOn));
                    sb.Append($"<a href=\"{httpIssuePrefix}{issue.Num}\"> {issue.Subject.EscapeHtml()}</a>\n");
                    sb.Append($"<i>Назначена: {issue.RedmineAssignOn.EscapeHtml()}</i>\n\n");
                }
            }

            return sb.ToString();

        }

        /// <summary> Define icon for user </summary>
        private string DefineRoleIconByUser(string? botUserId)
        {
            if (string.IsNullOrEmpty(botUserId))
            {
                return "㊙️";
            }

            return "🖥";
            //switch (usr.Role)
            //{
            //    case EnumUserRole.Developer:
            //        msg += "🖥";
            //        break;
            //    case EnumUserRole.Tester:
            //        msg += "⚽️";
            //        break;
            //    case EnumUserRole.Boss:
            //        msg += "💶";
            //        break;
            //    case EnumUserRole.Analist:
            //        msg += "📡";
            //        break;
            //    default:
            //        msg += "㊙️";
            //        break;
            //}
        }
    }
}