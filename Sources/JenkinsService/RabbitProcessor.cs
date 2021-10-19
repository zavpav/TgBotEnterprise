using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using JenkinsService.Database;
using JenkinsService.Jenkins;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using Serilog;

namespace JenkinsService.RabbitCommunication
{
    public class RabbitProcessor : IRabbitProcessor
    {
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IRabbitService _rabbitService;
        private readonly JenkinsDbContext _dbContext;
        private readonly INodeInfo _nodeInfo;
        private readonly JenkinsCommunication _jenkinsCommunication;

        public RabbitProcessor(INodeInfo nodeInfo, ILogger logger,
            IRabbitService rabbitService,
            IMapper mapper,
            JenkinsDbContext dbContext)
        {
            this._logger = logger;
            this._rabbitService = rabbitService;
            this._mapper = mapper;
            this._dbContext = dbContext;
            this._nodeInfo = nodeInfo;
            this._jenkinsCommunication = new JenkinsCommunication(logger);
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
                    return await this._jenkinsCommunication.GetAnyInformation();

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
            this._rabbitService.Subscribe<MainBotUpdateUserInfo>(EnumInfrastructureServicesType.Main,
                RabbitMessages.MainBotPublishUpdateUser,
                this.ProcessUpdateUserInformation,
                this._logger);

            this._rabbitService.Subscribe<WebAdminRequestProjectSettingsMessage>(EnumInfrastructureServicesType.WebAdmin,
                RabbitMessages.WebAdminProjectSettingsRequest,
                this.ProcessProjectSettingsRequest,
                this._logger);

            this._rabbitService.Subscribe<WebAdminUpdateProjectSettings>(EnumInfrastructureServicesType.WebAdmin,
                RabbitMessages.WebAdminProjectSettingsUpdate,
                this.ProcessProjectSettingsUpdate,
                this._logger);

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

        /// <summary> Processing request settings message  </summary>
        private async Task ProcessProjectSettingsRequest(WebAdminRequestProjectSettingsMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            var responseProjectSettings = new WebAdminResponseProjectSettingsMessage(message.SystemEventId,
                this._nodeInfo.ServicesType,
                this._nodeInfo.NodeName,
                "Jenkins settings");

            responseProjectSettings.SettingsItems = new[]
            {
                new WebAdminResponseProjectSettingsMessage.SettingsItem
                {
                    SystemName = "DumpJobName",
                    Description = "JobName for extracting dump",
                    SettingType = "string",
                    Value = ""
                },
                new WebAdminResponseProjectSettingsMessage.SettingsItem
                {
                    SystemName = "RcJobName",
                    Description = "JobName for build rc version",
                    SettingType = "string",
                    Value = ""
                },
                new WebAdminResponseProjectSettingsMessage.SettingsItem
                {
                    SystemName = "CurrentJobName",
                    Description = "JobName for build current version",
                    SettingType = "string",
                    Value = ""
                },
            };
            this._logger.Information(responseProjectSettings, "Response project settings {@message}", responseProjectSettings);

            await this._rabbitService.PublishInformation(RabbitMessages.WebAdminProjectSettingsResponse, responseProjectSettings);
        }

        /// <summary> Update settings from WebAdmin </summary>
        private Task ProcessProjectSettingsUpdate(WebAdminUpdateProjectSettings message, IDictionary<string, string> rabbitMessageHeaders)
        {
            if (!(message.ServicesType == this._nodeInfo.ServicesType && message.NodeName == this._nodeInfo.NodeName))
            {
                this._logger.Information(message, "Ignore message because not mine information {@incomeMessage}", message);
                return Task.CompletedTask;
            }

            this._logger.Information(message, "ProcessProjectSettingsUpdate {@message}", message);

            return Task.CompletedTask;
        }
    }
}