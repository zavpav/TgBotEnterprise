using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using JenkinsService.Database;
using JenkinsService.Jenkins;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.BuildService;
using RabbitMessageCommunication.Commmon;
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
        private readonly IGlobalEventIdGenerator _eventIdGenerator;
        private readonly IRabbitService _rabbitService;
        private readonly IDbContextFactory<JenkinsDbContext> _dbContextFactory;
        private readonly INodeInfo _nodeInfo;
        private readonly JenkinsCommunication _jenkinsCommunication;

        public RabbitProcessor(INodeInfo nodeInfo, ILogger logger,
            IRabbitService rabbitService,
            IMapper mapper,
            IGlobalEventIdGenerator eventIdGenerator,
            IDbContextFactory<JenkinsDbContext> dbContextFactory)
        {
            this._logger = logger;
            this._rabbitService = rabbitService;
            this._mapper = mapper;
            this._eventIdGenerator = eventIdGenerator;
            this._dbContextFactory = dbContextFactory;
            this._nodeInfo = nodeInfo;
            this._jenkinsCommunication = new JenkinsCommunication(logger, this._dbContextFactory);
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

            this._rabbitService.RegisterDirectProcessor<BuildServiceFindBuildByIssueNumRequestMessage, BuildServiceFindBuildByIssueNumResponseMessage>(
                RabbitMessages.BuildServiceFindBuildByIssueNum,
                this.ProcessBuildServiceFindBuildByIssueNum,
                this._logger);
        }

        private async Task<BuildServiceFindBuildByIssueNumResponseMessage> ProcessBuildServiceFindBuildByIssueNum(BuildServiceFindBuildByIssueNumRequestMessage message, IDictionary<string, string> rabbitmessageheaders)
        {
            await Task.Yield();
            await this._jenkinsCommunication.UpdateDb();

            await using var db = this._dbContextFactory.CreateDbContext();

#pragma warning disable CS8604 // Possible null reference argument.
            var res = await db.JenkinsJobs
                .AsNoTracking()
                .Include(x => x.ChangeInfos.Where(xx => xx.IssueId == message.IssueNum))
                .Where(x => x.ChangeInfos.Count(xx => xx.IssueId == message.IssueNum) != 0)
                .ToListAsync();
#pragma warning restore CS8604 // Possible null reference argument.

            if (res.Count == 0)
                return new BuildServiceFindBuildByIssueNumResponseMessage(message.SystemEventId, message.IssueNum);

            var buildCommentsInfos = res
                .SelectMany(x =>
                {

                    Debug.Assert(x.ChangeInfos != null, "x.ChangeInfos != null");
                    var commentsInfos = x.ChangeInfos?
                        .Select(xx => new BuildServiceFindBuildByIssueNumResponseMessage.BuildCommentsInfo
                        {
                            ProjectSysName = x.ProjectSysName,
                            JenkinsJobName = x.JenkinsJobName,
                            BuildNum = x.BuildNumber,
                            GitComment = xx.GitComment
                        });
                    return commentsInfos ?? Enumerable.Empty<BuildServiceFindBuildByIssueNumResponseMessage.BuildCommentsInfo>();
                })
                .ToArray();

            return new BuildServiceFindBuildByIssueNumResponseMessage(message.SystemEventId, message.IssueNum)
            {
                BuildCommentsInfos = buildCommentsInfos
            };
        }


        /// <summary> Send new job information </summary>
        /// <param name="jobsChanges">Information about new finished jobs</param>
        public async Task SendUpdatedJobs(List<DtoJobChanged> jobsChanges)
        {
            foreach (var jobChanged in jobsChanges)
            {
                var message = new BuildServiceBuildChangedMessage(this._eventIdGenerator.GetNextEventId());
                message.ArtifactsUri = jobChanged.ArtifactsUri;
                message.BuildUri = jobChanged.BuildUri;

                message.OldVersion = this._mapper.Map<BuildInfo>(jobChanged.OldBuildInfo);
                message.NewVersion = this._mapper.Map<BuildInfo>(jobChanged.NewBuildInfo);

                await this._rabbitService.PublishInformation(RabbitMessages.BuildSystemBuildChanged, message);
            }
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
        }

        private const string JobUrlPrefix = "JobUrl";
        private const string GitPrefixesProjectsSettingsName = "git:prefixesprojects";

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

            settings.Add(new WebAdminResponseProjectSettingsMessage.SettingsItem
            {
                SystemName = GitPrefixesProjectsSettingsName,
                Description = "Prefixes in comments for projects (commaseparated)",
                SettingType = "string",
                Value = projectSettings.GitProjectPrefixes
            });

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

            var gitProjectsPrefixes = message.SettingsItems.SingleOrDefault(x => x.SystemName == GitPrefixesProjectsSettingsName);
            projectSettings.GitProjectPrefixes = gitProjectsPrefixes == null ? "" : gitProjectsPrefixes.Value;
            // other settings

            await this._jenkinsCommunication.SaveProjectSettings(projectSettings);
        }

        /// <summary> Send problem to admin </summary>
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