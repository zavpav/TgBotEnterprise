using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.RabbitSimpleProcessors;
using RabbitMqInfrastructure;
using Serilog;
using TelegramService.Database;
using TelegramService.Telegram;

namespace TelegramService.RabbitCommunication
{
    public class RabbitProcessor : IRabbitProcessor
    {
        private readonly ILogger _logger;
        private readonly INodeInfo _nodeInfo;
        private readonly ITelegramWrap _telegramWrap;
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<TgServiceDbContext> _dbContextFactory;
        private readonly IRabbitService _rabbitService;

        public RabbitProcessor(ILogger logger,
            INodeInfo nodeInfo,
            IRabbitService rabbitService,
            ITelegramWrap telegramWrap,
            IMapper mapper,
            IDbContextFactory<TgServiceDbContext> dbContextFactory)
        {
            this._logger = logger;
            this._nodeInfo = nodeInfo;
            this._rabbitService = rabbitService;
            this._telegramWrap = telegramWrap;
            this._mapper = mapper;
            this._dbContextFactory = dbContextFactory;
        }

        public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit,
            string actionName,
            IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");

            await Task.Delay(9000);
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");
            return directMessage;
        }

        public void Subscribe()
        {
            this._rabbitService.Subscribe<TelegramOutgoingMessage>(EnumInfrastructureServicesType.Main,
                RabbitMessages.TelegramOutgoingMessage,
                this.ProcessOutgoingMessage,
                this._logger);

            this._rabbitService.Subscribe<TelegramOutgoingMessageHtml>(EnumInfrastructureServicesType.Main,
                RabbitMessages.TelegramOutgoingMessageHtml,
                this.ProcessOutgoingMessageHtml,
                this._logger);

            this._rabbitService.Subscribe<MainBotUpdateUserInfo>(EnumInfrastructureServicesType.Main,
                RabbitMessages.MainBotPublishUpdateUser,
                this.ProcessUpdateUserInformation,
                this._logger);

            this._rabbitService.Subscribe<TelegramOutgoingIssuesMessage>(EnumInfrastructureServicesType.Main, 
                RabbitMessages.TelegramOutgoingIssuesMessage,
                this.ProcessOutgoingIssueMessage,
                this._logger);

            this._rabbitService.Subscribe<TelegramOutgoingIssuesChangedMessage>(EnumInfrastructureServicesType.Main,
                RabbitMessages.TelegramOutgoingIssuesChangedMessage,
                this.ProcessOutgoingIssueChangedMessage,
                this._logger);

            this._rabbitService.Subscribe<TelegramOutgoingBuildChangedMessage>(EnumInfrastructureServicesType.Main,
                RabbitMessages.TelegramOutgoingBuildChanged,
                this.ProcessOutgoingBuildChangedMessage,
                this._logger);

            this._rabbitService.RegisterDirectProcessor(RabbitMessages.PingMessage, RabbitSimpleProcessors.DirectPingProcessor);
        }

        private Task ProcessOutgoingBuildChangedMessage(TelegramOutgoingBuildChangedMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            return this._telegramWrap.SendBuildChangedMessage(message);
        }

        private Task ProcessOutgoingIssueChangedMessage(TelegramOutgoingIssuesChangedMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            return this._telegramWrap.SendIssueChangedMessage(message);
        }

        private Task ProcessOutgoingIssueMessage(TelegramOutgoingIssuesMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            return this._telegramWrap.SendIssuesMessage(message);
        }

        private Task ProcessOutgoingMessage(TelegramOutgoingMessage messageData, IDictionary<string, string> rabbitMessageHeaders)
        {
            return this._telegramWrap.SendMessage(messageData);
        }

        private Task ProcessOutgoingMessageHtml(TelegramOutgoingMessageHtml messageData, IDictionary<string, string> rabbitMessageHeaders)
        {
            return this._telegramWrap.SendMessageHtml(messageData);
        }

        private async Task ProcessUpdateUserInformation(MainBotUpdateUserInfo message, IDictionary<string, string> rabbitMessageHeaders)
        {
            await using var db = this._dbContextFactory.CreateDbContext();

            var usrInfo = await db.UsersInfo
                .FirstOrDefaultAsync(x => x.BotUserId == (message.OriginalBotUserId ?? message.BotUserId));
            if (usrInfo == null)
            {
                this._logger.Information(message, "User doesn't exist {@newUserMessage}", message);
                var usr = this._mapper.Map<DbeUserInfo>(message);
                await db.UsersInfo.AddAsync(usr);
                await db.SaveChangesAsync();
            }
            else
            {
                this._logger.Information(message, "User exist {@oldUserInfo} {@newUserMessage}", usrInfo, message);
                usrInfo = this._mapper.Map(message, usrInfo);
                db.UsersInfo.Update(usrInfo);
                await db.SaveChangesAsync();
            }

            this._telegramWrap.ClearUserCache();
        }
    }
}