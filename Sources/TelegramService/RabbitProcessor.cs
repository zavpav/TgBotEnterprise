using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
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
        private readonly TgServiceDbContext _dbContext;
        private readonly IRabbitService _rabbitService;

        public RabbitProcessor(ILogger logger,
            INodeInfo nodeInfo,
            IRabbitService rabbitService,
            ITelegramWrap telegramWrap,
            IMapper mapper,
            TgServiceDbContext dbContext)
        {
            this._logger = logger;
            this._nodeInfo = nodeInfo;
            this._rabbitService = rabbitService;
            this._telegramWrap = telegramWrap;
            this._mapper = mapper;
            this._dbContext = dbContext;
        }

        public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit,
            string actionName,
            IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");

            if (actionName.ToUpper() == "PING")
            {
                try
                {
                    return "Alive. Don't direct tested.";

                }
                catch (Exception e)
                {
                    return "Error " + e.ToString();
                }
            }
            else
            {
                await Task.Delay(9000);
                Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");
                return directMessage;
            }
        }

        public void Subscribe()
        {
            this._rabbitService.Subscribe<TelegramOutgoingMessage>(EnumInfrastructureServicesType.Main,
                RabbitMessages.TelegramOutgoingMessage,
                this.ProcessOutgoingMessage,
                this._logger);

            this._rabbitService.Subscribe<MainBotUpdateUserInfo>(EnumInfrastructureServicesType.Main,
                RabbitMessages.MainBotPublishUpdateUser,
                this.ProcessUpdateUserInformation,
                this._logger);

        }

        private Task ProcessOutgoingMessage(TelegramOutgoingMessage messageData, IDictionary<string, string> rabbitMessageHeaders)
        {
            return this._telegramWrap.SendMessage(messageData);
        }

        private async Task ProcessUpdateUserInformation(MainBotUpdateUserInfo message, IDictionary<string, string> rabbitMessageHeaders)
        {
            var usrInfo = await this._dbContext.UsersInfo
                .FirstOrDefaultAsync(x => x.BotUserId == (message.OriginalBotUserId ?? message.BotUserId));
            if (usrInfo == null)
            {
                this._logger.Information(message, "User doesn't exist {@newUserMessage}", message);
                var usr = this._mapper.Map<DtoUserInfo>(message);
                await this._dbContext.UsersInfo.AddAsync(usr);
                await this._dbContext.SaveChangesAsync();
            }
            else
            {
                this._logger.Information(message, "User exist {@oldUserInfo} {@newUserMessage}", usrInfo, message);
                usrInfo = this._mapper.Map(message, usrInfo);
                this._dbContext.UsersInfo.Update(usrInfo);
                await this._dbContext.SaveChangesAsync();
            }

            this._telegramWrap.ClearUserCache();
        }
    }
}