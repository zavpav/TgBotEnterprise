using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
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
                                    }
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




        public async Task SendMessage(TelegramOutgoingMessage messageData)
        {
            this._logger.Information(messageData, "Sending message to user {@messageData}", messageData);
            var msg = await this._telegramBot.SendTextMessageAsync(messageData.ChatId, messageData.Message, replyToMessageId: messageData.MessageId ?? 0);
            //msg.MessageId
        }
    }
}