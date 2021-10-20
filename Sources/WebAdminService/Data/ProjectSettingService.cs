using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using RabbitMessageCommunication;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using Serilog;

namespace WebAdminService.Data
{

    public delegate Task ProcessMessage(string eventId, WebAdminResponseProjectSettingsMessage message);


    /// <summary> Service for project settings  </summary>
    public class ProjectSettingService
    {
        private readonly IRabbitService _rabbitService;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IGlobalEventIdGenerator _eventIdGenerator;

        /// <summary> Cache for response waters </summary>
        private readonly System.Runtime.Caching.MemoryCache _waitResponseCache = new System.Runtime.Caching.MemoryCache("responseWaiter");

        public ProjectSettingService(
            IRabbitService rabbitService,
            ILogger logger,
            IMapper mapper,
            IGlobalEventIdGenerator eventIdGenerator)
        {
            this._rabbitService = rabbitService;
            this._logger = logger;
            this._mapper = mapper;
            this._eventIdGenerator = eventIdGenerator;


            this.SubscribeNeededMessages();
        }

        /// <summary> Get main information about all projects from mainbot </summary>
        public async Task<ProjectSettingsMainInfoPresentor[]> GetProjectsListAsync()
        {
            var emptyMessage = new MainBotProjectInfoRequestMessage(this._eventIdGenerator.GetNextEventId());
            var allProjectsResponse = await this._rabbitService.DirectRequestToMainBot<MainBotProjectInfoRequestMessage, MainBotProjectInfoResponseMessage>(RabbitMessages.MainBotProjectsInfoRequest, emptyMessage);
            var projects = this._mapper.Map<ProjectSettingsMainInfoPresentor[]>(allProjectsResponse.AllProjectInfos) 
                           ?? new ProjectSettingsMainInfoPresentor[] { };
            return projects;
        }

        /// <summary> Main information about project </summary>
        public class ProjectSettingsMainInfoPresentor
        {
            /// <summary> System name of project </summary>
            public string SysName { get; set; }

            /// <summary> Name for people </summary>
            public string Description { get; set; }

            /// <summary> Current version Number (as in Redmine) </summary>
            public string? CurrentVersion { get; set; }

            /// <summary> Rc version Number (as in Redmine) </summary>
            public string? RcVersion { get; set; }

        }

        /// <summary> Subscribe for needed messages </summary>
        /// <remarks>
        ///  TODO Maybe to move to RabbitProcessor...
        /// </remarks>
        private void SubscribeNeededMessages()
        {
            this._rabbitService.Subscribe<WebAdminResponseProjectSettingsMessage>(null,
                RabbitMessages.WebAdminProjectSettingsResponse,
                this.ProcessResponseRequestedProjectSettings,
                this._logger
            );
        }

        /// <summary> Process subscribed information in RequestProjectSettings </summary>
        private async Task ProcessResponseRequestedProjectSettings(WebAdminResponseProjectSettingsMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            var eventId = message.SystemEventId;

            var processor = (ProcessMessage) this._waitResponseCache.Get(eventId);
            if (processor == null)
                this._logger.Error(message, "Received message with rotten cache {@message}", message);
            else
            {
                this._logger.Information(message, "Processing message with cache processor {@message}", message);

                await processor(eventId, message);
            }
        }

        /// <summary> Get information about single project </summary>
        public async Task<ProjectSettingsMainInfoPresentor> GetSingleProject(string projectSysName)
        {
            var requestMessage = new MainBotProjectInfoRequestMessage(this._eventIdGenerator.GetNextEventId())
            {
                ProjectSysName = projectSysName
            };
            var projectResponse = await this._rabbitService.DirectRequestToMainBot<MainBotProjectInfoRequestMessage, MainBotProjectInfoResponseMessage>(RabbitMessages.MainBotProjectsInfoRequest, requestMessage);
            var projects = this._mapper.Map<ProjectSettingsMainInfoPresentor[]>(projectResponse.AllProjectInfos)
                           ?? new ProjectSettingsMainInfoPresentor[] { };

            if (projects.Length != 1)
            {
                this._logger.Error(projectResponse, "Received error information {@projects}", (object)projects); //casting for sending array as a single value
                throw new NotSupportedException("Received error information");
            }

            return projects[0];
        }



        /// <summary> Subscribe for information about needed settings *from WebPage* and send request for this information </summary>
        /// <param name="projectSysName"></param>
        /// <param name="onNeedSettings"></param>
        /// <returns>Request eventId</returns>
        /// <remarks>
        ///    This class is a singleton and may serve some pages at the same time.
        /// </remarks>
        public async Task<string> RequestProjectSettings(string projectSysName, ProcessMessage onNeedSettings)
        {
            var eventId = this._eventIdGenerator.GetNextEventId();
            var message = new WebAdminRequestProjectSettingsMessage(eventId, projectSysName);
            var policy = new System.Runtime.Caching.CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(60.0)
            };

            var cacheItem = new System.Runtime.Caching.CacheItem(eventId, onNeedSettings);
            this._waitResponseCache.Add(cacheItem, policy);

            await this._rabbitService.PublishInformation(RabbitMessages.WebAdminProjectSettingsRequest, message);

            return eventId;
        }


        /// <summary> Save settings through all services  </summary>
        /// <param name="projectInfo">Information about single project</param>
        /// <param name="settings">Setting split by all services</param>
        /// <returns></returns>
        public async Task SaveSettings(ProjectSettingsMainInfoPresentor projectInfo, List<WebAdminResponseProjectSettingsMessage> settings)
        {
            this._logger.Information("Update project settings");
            
            var mbProjectInfo = this._mapper.Map<MainBotProjectInfo>(projectInfo);
            var updateMainProjectInformation = new MainBotProjectInfoUpdateMessage(this._eventIdGenerator.GetNextEventId(), mbProjectInfo);
            await this._rabbitService.PublishInformation(RabbitMessages.MainBotProjectSettingsUpdate, updateMainProjectInformation);

            foreach (var setting in settings)
            {
                var updSettings = this._mapper.Map<WebAdminUpdateProjectSettings.SettingsItem[]>(setting.SettingsItems);

                var updateServiceSettings = new WebAdminUpdateProjectSettings(this._eventIdGenerator.GetNextEventId(),
                    setting.ServicesType,
                    setting.NodeName,
                    projectInfo.SysName,
                    updSettings);

                await this._rabbitService.PublishInformation(RabbitMessages.WebAdminProjectSettingsUpdate, updateServiceSettings);
            }
        }

    }
}