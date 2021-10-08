using System;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using MainBotService.Database;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using Serilog;

namespace MainBotService
{
    public partial class MainBotWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IRabbitService _rabbitService;
        private readonly IMapper _mapper;
        private readonly BotServiceDbContext _dbContext;

        private readonly TelegramProcessor _telegramProcessor;
        private readonly RedmineProcessor _redmineProcessor;

        public MainBotWorker(ILogger logger, 
            IRabbitService rabbitService,
            IMapper mapper, 
            BotServiceDbContext dbContext)
        {
            this._logger = logger;
            this._rabbitService = rabbitService;
            this._mapper = mapper;
            this._dbContext = dbContext;

            this._telegramProcessor = new TelegramProcessor(this);
            this._redmineProcessor = new RedmineProcessor(this);
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
                this.ProcessIncomeTelegramMessage, 
                this._logger);

            this._rabbitService.Subscribe<TelegramPublishNewUserFromTelegram>(EnumInfrastructureServicesType.Messaging,
                RabbitMessages.TelegramPublishNewUserFromTelegram,
                this.ProcessNewUserFromTelegram,
                this._logger);

            this._rabbitService.Subscribe<WebAdminUpdateUserInfo>(EnumInfrastructureServicesType.WebAdmin,
                RabbitMessages.WebAdminPublishUpdateUser,
                this.ProcessUpdateUserFromWebAdmin,
                this._logger);


        }

        #endregion

        /// <summary> Process a income telegram message. </summary>
        /// <param name="incomeMessage">User message</param>
        /// <param name="rabbitMessageHeaders">Rabbit headers</param>
        private async Task ProcessIncomeTelegramMessage(TelegramIncomeMessage incomeMessage, IDictionary<string, string> rabbitMessageHeaders)
        {
            await this.UpdateUserInfoFromTelegram(incomeMessage.BotUserId);

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

        #region User data updates
        
        /// <summary> Update local databse from message if user doesn't exist</summary>
        private async Task UpdateUserInfoFromTelegram(string botUserId)
        {
            var userExists = this._dbContext.UsersInfo.Any(x => x.BotUserId == botUserId);
            if (!userExists)
            {
                var userInfo = new DtoUserInfo { BotUserId = botUserId, WhoIsThis = botUserId };
                this._dbContext.UsersInfo.Add(userInfo);
                await this._dbContext.SaveChangesAsync();
            }
        }

        /// <summary> Processing event about new user from telegram message </summary>
        /// <param name="newUserInfo">Information about user</param>
        /// <param name="rabbitMessageHeaders">Rabbit headers</param>
        private async Task ProcessNewUserFromTelegram(TelegramPublishNewUserFromTelegram newUserInfo, 
            IDictionary<string, string> rabbitMessageHeaders)
        {
            await this.ProcessUpdateUser(newUserInfo.BotUserId, 
                newUserInfo.SystemEventId,
                usr =>
                    {
                        if (usr != null)
                        {
                            usr.WhoIsThis ??= newUserInfo.WhoIsThis;
                        }
                        else
                        {
                            usr = new DtoUserInfo
                            {
                                BotUserId = newUserInfo.BotUserId,
                                WhoIsThis = newUserInfo.WhoIsThis
                            };
                        }

                        return usr;
                    }
                );
        }

        /// <summary> Process user information from web admin </summary>
        /// <param name="newUserInfo">Information about user</param>
        /// <param name="rabbitMessageHeaders">Rabbit headers</param>
        private async Task ProcessUpdateUserFromWebAdmin(WebAdminUpdateUserInfo newUserInfo, IDictionary<string, string> rabbitMessageHeaders)
        {
            await this.ProcessUpdateUser(newUserInfo.OriginalBotUserId ?? newUserInfo.BotUserId, 
                newUserInfo.SystemEventId, 
                usr =>
                    {
                        if (usr == null)
                            usr = new DtoUserInfo();

                        usr = this._mapper.Map(newUserInfo, usr);
                        
                        return usr;
                    }
                );
        }

        /// <summary> Process user information </summary>
        private async Task ProcessUpdateUser(string findBotId, string eventId, Func<DtoUserInfo?, DtoUserInfo> updateFunc)
        {
            var user = await this._dbContext.UsersInfo.FirstOrDefaultAsync(x => x.BotUserId == findBotId);
            if (user != null) // Maybe exists, if telegram message process first
            {
                user = updateFunc(user);

                this._logger.InformationWithEventContext(eventId,
                    "MainBot user exists. Try Update from telegram information {BotUserId}->{@UserInfoNew}",
                    findBotId,
                    user
                );

                this._dbContext.UsersInfo.Update(user);
                await this._dbContext.SaveChangesAsync();
            }
            else
            {
                user = updateFunc(null);

                this._logger.InformationWithEventContext(eventId,
                    "MainBot user doesn't exist. New user from telegram information {BotUserId}->{@UserInfoNew}",
                    findBotId,
                    user
                );

                await this._dbContext.UsersInfo.AddAsync(user);
                await this._dbContext.SaveChangesAsync();
            }

            // Send user update message to all subscribers
            user = await this._dbContext.UsersInfo.FirstAsync(x => x.BotUserId == user.BotUserId);
            var updUserMessage = this._mapper.Map<MainBotUpdateUserInfo>(user);
            updUserMessage.SystemEventId = eventId;
            updUserMessage.OriginalBotUserId = findBotId;

            // Publish information for all
            await this._rabbitService.PublishInformation(RabbitMessages.MainBotPublishUpdateUser, updUserMessage);
        }

        #endregion
    }
}
