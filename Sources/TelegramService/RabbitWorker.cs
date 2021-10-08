using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
using RabbitMqInfrastructure;
using Serilog;
using TelegramService.Database;
using TelegramService.Telegram;

namespace TelegramService
{
    public class RabbitWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ITelegramWrap _telegramWrap;
        private readonly IMapper _mapper;
        private readonly TgServiceDbContext _dbContext;
        private readonly IRabbitService _rabbitService;

        public RabbitWorker(ILogger logger, 
            IRabbitService rabbitService, 
            ITelegramWrap telegramWrap,
            IMapper mapper,
            TgServiceDbContext dbContext)
        {
            this._logger = logger;
            this._rabbitService = rabbitService;
            this._telegramWrap = telegramWrap;
            this._mapper = mapper;
            this._dbContext = dbContext;
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
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._rabbitService.Subscribe<TelegramOutgoingMessage>(EnumInfrastructureServicesType.Main,
                RabbitMessages.TelegramOutgoingMessage,
                this.ProcessOutgoingMessage,
                this._logger);

            this._rabbitService.Subscribe<MainBotUpdateUserInfo>(EnumInfrastructureServicesType.Main,
                RabbitMessages.MainBotPublishUpdateUser,
                this.ProcessUpdateUserInformation,
                this._logger);


            while (!stoppingToken.IsCancellationRequested)
            {
                await this._telegramWrap.Pull();
                //                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

    }
}