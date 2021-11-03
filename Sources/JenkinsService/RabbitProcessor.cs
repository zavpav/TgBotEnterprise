﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using JenkinsService.Database;
using JenkinsService.Jenkins;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.RabbitSimpleProcessors;
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
            this._jenkinsCommunication = new JenkinsCommunication(logger, this._dbContext);
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

            this._rabbitService.RegisterDirectProcessor(RabbitMessages.PingMessage, RabbitSimpleProcessors.DirectPingProcessor);
        }

        private async Task ProcessUpdateUserInformation(MainBotUpdateUserInfo message, IDictionary<string, string> rabbitMessageHeaders)
        {
            var usrInfo = await this._dbContext.UsersInfo
                .FirstOrDefaultAsync(x => x.BotUserId == (message.OriginalBotUserId ?? message.BotUserId));
            if (usrInfo == null)
            {
                this._logger.Information(message, "User doesn't exist {@newUserMessage}", message);
                var usr = this._mapper.Map<DbeUserInfo>(message);
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

        private const string JobUrlPrefix = "JobUrl";

        /// <summary> Processing request settings message  </summary>
        private async Task ProcessProjectSettingsRequest(WebAdminRequestProjectSettingsMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            var projectSettings = await this._jenkinsCommunication.GetProjectSettings(message.ProjectSysName, true);

            if (projectSettings == null)
            {
                this._logger.Error(message, "Getting project settings error projectSettings == null");
                throw new NotSupportedException("Getting project settings error projectSettings == null");
            }

            var responseProjectSettings = new WebAdminResponseProjectSettingsMessage(message.SystemEventId,
                this._nodeInfo.ServicesType,
                this._nodeInfo.NodeName,
                "Jenkins settings",
                projectSettings.ProjectSysName);


            var settings = new List<WebAdminResponseProjectSettingsMessage.SettingsItem>();
            
            // Update jobs info
            foreach (var jobDescription in projectSettings.JobInformations.OrderBy(x => x.JobType))
            {
                var sett = new WebAdminResponseProjectSettingsMessage.SettingsItem
                {
                    SettingType = "string",
                    SystemName = $"{JobUrlPrefix}:{jobDescription.JobType}",
                    Value = jobDescription.JobPath
                };

                var descrAttr = typeof(EnumBuildServerJobs)
                    .GetField(sett.SystemName)
                    ?.GetCustomAttribute(typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descrAttr == null)
                    sett.Description = "Url part for " + sett.SystemName;
                else
                    sett.Description = "Url part for " + descrAttr.Description;
                
                settings.Add(sett);
            }

            responseProjectSettings.SettingsItems = settings.ToArray();

            this._logger.Information(responseProjectSettings, "Response project settings {@message}", responseProjectSettings);

            await this._rabbitService.PublishInformation(RabbitMessages.WebAdminProjectSettingsResponse, responseProjectSettings);
        }

        /// <summary> Update settings from WebAdmin </summary>
        private async Task ProcessProjectSettingsUpdate(WebAdminUpdateProjectSettings message, IDictionary<string, string> rabbitMessageHeaders)
        {
            if (!(message.ServicesType == this._nodeInfo.ServicesType && message.NodeName == this._nodeInfo.NodeName))
            {
                this._logger.Information(message, "Ignore message because it's not my information {@incomeMessage}", message);
                return;
            }

            this._logger.Information(message, "ProcessProjectSettingsUpdate {@message}", message);
            var projectSettings = await this._jenkinsCommunication.GetProjectSettings(message.ProjectSysName, true);

            if (projectSettings == null)
            {
                this._logger.Error(message, "Getting project settings error projectSettings == null");
                throw new NotSupportedException("Getting project settings error projectSettings == null");
            }

            // Update jobs info
            foreach (var jobType in Enum.GetValues<EnumBuildServerJobs>()
                    .Where(x => x != EnumBuildServerJobs.Undef))
            {
                var singleJobInfo = projectSettings.JobInformations.Single(x => x.JobType == jobType);

                var messageSettingJob = message.SettingsItems.Where(x => x.SystemName.EndsWith(":" + jobType)).ToList();
                if (messageSettingJob.Count == 0 || messageSettingJob.All(x => string.IsNullOrEmpty(x.Value)))
                {
                    projectSettings.JobInformations.Remove(singleJobInfo);
                }
                else
                {
                    var jobPath = messageSettingJob.SingleOrDefault(x => x.SystemName.StartsWith(JobUrlPrefix));
                    singleJobInfo.JobPath = jobPath?.Value ?? "";
                }
            }
            // other settings

            await this._jenkinsCommunication.SaveProjectSettings(projectSettings);
        }
    }
}