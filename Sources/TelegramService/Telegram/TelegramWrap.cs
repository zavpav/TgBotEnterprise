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
    }

    public class TelegramWrap : ITelegramWrap
    {
        private readonly ILogger _logger;
        private readonly TgServiceDbContext _dbContext;
        private readonly ITelegramBotClient _telegramBot;
        private readonly IRabbitService _rabbitService;
        private readonly IGlobalIncomeIdGenerator _globalIncomeIdGenerator;

        private int _tgMessageOffset;

        public TelegramWrap(ILogger logger,
            ITelegramBotClient telegramBot, 
            IRabbitService rabbitService,
            IGlobalIncomeIdGenerator globalIncomeIdGenerator,
            TgServiceDbContext dbContext)
        {
            this._logger = logger;
            this._dbContext = dbContext;
            this._telegramBot = telegramBot;
            this._rabbitService = rabbitService;
            this._globalIncomeIdGenerator = globalIncomeIdGenerator;
        }

        public async Task Pull()
        {
            var messages = await this._telegramBot.GetUpdatesAsync(offset: this._tgMessageOffset);

            if (messages.Length != 0)
            {
                foreach (var msg in messages)
                {
                    var incomeId = this._globalIncomeIdGenerator.GetNextIncomeId();
                    var tgMsg = msg.Message ?? msg.EditedMessage;
                    if (tgMsg != null)
                    {
                        var messageData = new TelegramIncomeMessage
                        {
                            UpdateId = msg.Id,
                            IncomeId = incomeId,
                            ChatId = tgMsg.Chat.Id,
                            IsDirectMessage = tgMsg.Chat.Type == ChatType.Private,
                            MessageText = tgMsg.Text,
                            MessageId = tgMsg.MessageId,
                            TelegramUserId = tgMsg.From.Id,
                            BotUserId = await this.DefineBotUserId(tgMsg.From),
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

        private ConcurrentBag<UserCache> _usersCache = new ConcurrentBag<UserCache>();

        private async Task<string> DefineBotUserId(User telegramUser)
        {
            var usr = this._usersCache.FirstOrDefault(x => x.TelegramUserId == telegramUser.Id);

            if (usr.Equals(default))
            {
                var usrInfo = await this._dbContext.UsersInfo.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUser.Id);
                if (usrInfo?.BotUserId != null)
                {
                    // Exists activated user
                    if (usrInfo.IsActive)
                        this._usersCache.Add(new UserCache(usrInfo.TelegramUserId, usrInfo.DefaultChatId, usrInfo.BotUserId));

                    return usrInfo.BotUserId;
                }
            }

            Console.WriteLine($"Undefined user:  UserName: {telegramUser.Username} LastName: {telegramUser.LastName} FirstName: {telegramUser.FirstName}");
            return GlobalConstants.UndefinedBotUserId;
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
            var msg = await this._telegramBot.SendTextMessageAsync(messageData.ChatId, messageData.Message, replyToMessageId: messageData.MessageId ?? 0);
            //msg.MessageId
        }
    }
}