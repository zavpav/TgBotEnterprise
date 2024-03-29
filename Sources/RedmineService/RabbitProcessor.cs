﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.BugTracker;
using RabbitMessageCommunication.Commmon;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.RabbitSimpleProcessors;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using RedmineService.Database;
using RedmineService.Redmine;
using Serilog;

namespace RedmineService
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RabbitProcessor: IRabbitProcessor
    {
        private readonly INodeInfo _nodeInfo;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IRabbitService _rabbitService;
        private readonly IDbContextFactory<RedmineDbContext> _dbContextFactory;
        private readonly IGlobalEventIdGenerator _eventIdGenerator;

        private readonly RedmineCommunication _redmineService;

        public RabbitProcessor(INodeInfo nodeInfo,
            ILogger logger,
            IRabbitService rabbitService,
            IMapper mapper,
            IDbContextFactory<RedmineDbContext> dbContextFactory, 
            IGlobalEventIdGenerator eventIdGenerator)
        {
            this._nodeInfo = nodeInfo;
            this._logger = logger;
            this._rabbitService = rabbitService;
            this._mapper = mapper;
            this._dbContextFactory = dbContextFactory;
            this._eventIdGenerator = eventIdGenerator;

            this._redmineService = new RedmineCommunication(logger, dbContextFactory);
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

            this._rabbitService.RegisterDirectProcessor<BugTrackerTasksRequestMessage, BugTrackerTasksResponseMessage>(
                RabbitMessages.BugTrackerRequestIssues, 
                this.ProcessBugTrackerRequestIssues,
                this._logger);
        }

        #region Processing messages

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
        }

        /// <summary> Processing request settings message  </summary>
        private async Task ProcessProjectSettingsRequest(WebAdminRequestProjectSettingsMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            var responseProjectSettings = new WebAdminResponseProjectSettingsMessage(message.SystemEventId,
                this._nodeInfo.ServicesType,
                this._nodeInfo.NodeName,
                "Redmine settings",
                message.ProjectSysName);

            var projectSettings = await this._redmineService.GetProjectSettings(message.ProjectSysName, true);

            if (projectSettings == null)
            {
                this._logger.Error(message, "Getting project settings error projectSettings == null");
                throw new NotSupportedException("Getting project settings error projectSettings == null");
            }

            responseProjectSettings.SettingsItems = new[]
            {
                new WebAdminResponseProjectSettingsMessage.SettingsItem
                {
                    SystemName = nameof(projectSettings.RedmineProjectName),
                    Description = "Name of project in redmine service",
                    SettingType = "string",
                    Value = projectSettings.RedmineProjectName
                },
                new WebAdminResponseProjectSettingsMessage.SettingsItem
                {
                    SystemName = nameof(projectSettings.VersionMask),
                    Description = "Version submask for project",
                    SettingType = "string",
                    Value = projectSettings.VersionMask
                }
            };
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

            var projectSettings = await this._redmineService.GetProjectSettings(message.ProjectSysName, true);
            if (projectSettings == null)
            {
                this._logger.Error(message, "Getting project settings error projectSettings == null");
                throw new NotSupportedException("Getting project settings error projectSettings == null");
            }

            projectSettings.RedmineProjectName = message.SettingsItems
                                                     .SingleOrDefault(x => x.SystemName == nameof(projectSettings.RedmineProjectName))?.Value ?? "";
            projectSettings.VersionMask = message.SettingsItems
                                              .SingleOrDefault(x => x.SystemName == nameof(projectSettings.VersionMask))?.Value ?? "";

            await this._redmineService.SaveProjectSettings(projectSettings);
        }

        #endregion

        #region Process direct requests
        
        /// <summary> Get messages list by "filter" </summary>
        private async Task<BugTrackerTasksResponseMessage> ProcessBugTrackerRequestIssues(BugTrackerTasksRequestMessage requestMessage, IDictionary<string, string> rabbitMessageHeaders)
        {
            var responseMessage = new BugTrackerTasksResponseMessage(requestMessage.SystemEventId);

            // force update issues
            var changedIssues = await this._redmineService.UpdateIssuesDb();
            Task? sendUpdatedIssues = null;
            if (changedIssues.Count > 0)
                sendUpdatedIssues = this.SendUpdatedIssues(changedIssues);

            var foundIssues = await this._redmineService.SimpleFindIssues(
                requestMessage.IssueNums,
                requestMessage.FilterUserBotId,
                requestMessage.FilterProjectSysName, 
                requestMessage.FilterVersionText);

            responseMessage.Issues = this._mapper.Map<BugTrackerIssue[]>(foundIssues);

            if (requestMessage.IssueNums != null)
            {
                var htmlPrefix = await this._redmineService.GetHttpPrefixOfIssue();
                responseMessage.Issues.ForEach(x => x.IssueUrl = htmlPrefix + x.Num);
            }

            if (sendUpdatedIssues != null)
                await sendUpdatedIssues;

            return responseMessage;
        }

        /// <summary> Send information about changed issues </summary>
        public async Task SendUpdatedIssues(List<DtoIssueChanged> changedIssues)
        {
            // I don't think I often get many messages at once.
            // That's why I don't concat several messages into one.
            foreach (var issueChanged in changedIssues)
            {
                try
                {
                    var changedIssueMessage = new BugTrackerIssueChangedMessage(this._eventIdGenerator.GetNextEventId());
                    var urlPrefix = await this._redmineService.GetHttpPrefixOfIssue();

                    if (issueChanged.OldVersion != null)
                    {
                        changedIssueMessage.OldVersion = this._mapper.Map<BugTrackerIssue>(issueChanged.OldVersion);
                        changedIssueMessage.OldVersion.IssueUrl = urlPrefix + changedIssueMessage.OldVersion.Num;
                    }

                    if (issueChanged.NewVersion != null)
                    {
                        changedIssueMessage.NewVersion = this._mapper.Map<BugTrackerIssue>(issueChanged.NewVersion);
                        changedIssueMessage.NewVersion.IssueUrl = urlPrefix + changedIssueMessage.NewVersion.Num;
                    }

                    this._mapper.Map(issueChanged, changedIssueMessage);

                    this._logger
                        .ForContext("message", changedIssueMessage, true)
                        .Information("Issue changed #{Num}",
                            changedIssueMessage.OldVersion?.Num
                            ?? changedIssueMessage.NewVersion?.Num
                            ?? "<error>");

                    await this._rabbitService.PublishInformation(
                        RabbitMessages.BugTrackerIssueChanged,
                        changedIssueMessage,
                        EnumInfrastructureServicesType.Main);
                }
                catch (Exception e)
                {
                    this._logger.ForContext("changed", issueChanged, true)
                        .Warning(e, "Error during sending RedmineChangingMessage");
                }
            }
        }

        public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit,
            string actionName,
            IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");

            if (actionName.ToUpper() == "ANY_QUERY")
            {
                try
                {
                    return (await this._redmineService.GetAnyInformation());
                }
                catch (Exception e)
                {
                    return "Error: " + e.Message + " " + e.ToString();

                }
            }

            return "Response for " + directMessage;
        }
        #endregion

        public async Task SendProblemToAdmin(Exception exception)
        {
            var message = new ServiceProblemMessage(this._eventIdGenerator.GetNextEventId(), this._nodeInfo.ServicesType, this._nodeInfo.NodeName)
            {
                ExceptionTypeName = exception.GetType().FullName,
                ExceptionString = exception.ToString(),
                ExceptionStackTrace = exception.StackTrace
            };

            try
            {
                await this._rabbitService.PublishInformation(RabbitMessages.ServiceProblem, message, EnumInfrastructureServicesType.Main);
            }
            catch (Exception e)
            {
                this._logger.Error(e, "Error while send error information");
                throw;
            }
        }
    }
}