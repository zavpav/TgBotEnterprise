using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using MainBotService.Database;
using MainBotService.RabbitCommunication.Telegram;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.BugTracker;
using RabbitMessageCommunication.BuildService;
using RabbitMessageCommunication.Commmon;
using RabbitMessageCommunication.MainBot;
using RabbitMessageCommunication.RabbitSimpleProcessors;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using Serilog;
using TelegramService.RabbitCommunication;

namespace MainBotService.RabbitCommunication
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class MainBotService : IRabbitProcessor, IMainBotService
    {
        private readonly INodeInfo _nodeInfo;
        private readonly ILogger _logger;
        private readonly IGlobalEventIdGenerator _eventIdGenerator;
        private readonly IRabbitService _rabbitService;
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<BotServiceDbContext> _dbContextFactory;

        private readonly TelegramProcessor _telegramProcessor;
        private readonly RedmineProcessor _redmineProcessor;

        public MainBotService(INodeInfo nodeInfo, 
            ILogger logger,
            IGlobalEventIdGenerator eventIdGenerator,
            IRabbitService rabbitService,
            IMapper mapper,
            Lazy<IEnumerable<ITelegramConversation>> telegraConversations,
            IDbContextFactory<BotServiceDbContext> dbContextFactory)
        {
            this._nodeInfo = nodeInfo;
            this._logger = logger;
            this._eventIdGenerator = eventIdGenerator;
            this._rabbitService = rabbitService;
            this._mapper = mapper;
            this._dbContextFactory = dbContextFactory;

            this._telegramProcessor = new TelegramProcessor(this, telegraConversations, this._logger);
            this._redmineProcessor = new RedmineProcessor(this, this._logger);
        }

        public IRabbitService RabbitService() => this._rabbitService;

        public void Subscribe()
        {
            this._rabbitService.Subscribe<ServiceProblemMessage>(null, 
                RabbitMessages.ServiceProblem,
                this.ProcessServiceProblem,
                this._logger);

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

            this._rabbitService.RegisterDirectProcessor(RabbitMessages.PingMessage, RabbitSimpleProcessors.DirectPingProcessor);

            this._rabbitService.RegisterDirectProcessor<EmptyMessage, ResponseAllUsersMessage>(
                RabbitMessages.MainBotDirectGetAllUsers,
                this.ProcessMainBotDirectGetAllUsers,
                this._logger);

            this._rabbitService.Subscribe<BugTrackerIssueChangedMessage>(EnumInfrastructureServicesType.BugTracker,
                RabbitMessages.BugTrackerIssueChanged,
                this.ProcessBugTrackerIssueChanged,
                this._logger);

            this._rabbitService.Subscribe<BuildServiceBuildChangedMessage>(EnumInfrastructureServicesType.BuildService,
                RabbitMessages.BuildSystemBuildChanged,
                this.ProcessBuildSystemBuildChanged,
                this._logger);
        }


        /// <summary>
        /// TODO MainAdminID TEST
        /// </summary>
        private string _adminId = "zavjalov";

        private
            ConcurrentDictionary<Tuple<string?, EnumInfrastructureServicesType , string >, DateTime> _serviceProblem 
                = new ConcurrentDictionary<Tuple<string?, EnumInfrastructureServicesType, string>, DateTime>();

        /// <summary> Process a big problem from any service </summary>
        private async Task ProcessServiceProblem(ServiceProblemMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            this._logger.Error("ServiceError {@serviceProblem}", message);

            // Now I don't want to know any problem in a night :)
            if (DateTime.Now.TimeOfDay < new TimeSpan(0, 11, 0))
                return;

            var key = Tuple.Create(message.ExceptionTypeName, message.ServicesType, message.NodeName);

            this._serviceProblem.TryGetValue(key, out var lastUpdate);

            // I don't want to get a lot information
            if (DateTime.Now - lastUpdate > new TimeSpan(0, 20, 0))
            {
                var outgoingMessage = new TelegramOutgoingMessage(message.SystemEventId, 
                    $"Problem with service {message.ServicesType} {message.NodeName}\n\n" +
                             "Description: " + message.Description + "\n\n" +
                             "Exception type: " + message.ExceptionTypeName + "\n\n" +
                             "ExceptionInfo:\n" + message.ExceptionString + "\n\n" +
                             "StackTrace:\n" + message.ExceptionStackTrace)
                {
                    BotUserId = this._adminId,
                };

                await this._rabbitService.PublishInformation(RabbitMessages.TelegramOutgoingMessage,
                    outgoingMessage, EnumInfrastructureServicesType.Messaging);

                lastUpdate = DateTime.Now;
            }

            this._serviceProblem.AddOrUpdate(key,
                k => lastUpdate,
                (k, v) => lastUpdate
                );
        }


        public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit, 
            string actionName, 
            IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            await using var db = this._dbContextFactory.CreateDbContext();

            this._logger.Information("Untrack Info Direct request {actionName} message data {directMessage}", actionName, directMessage);

            if (actionName.ToUpper() == RabbitMessages.MainBotProjectsInfoRequest.ToUpper())
            {
                var requestProjectsMessage = JsonSerializer2.DeserializeRequired<MainBotProjectInfoRequestMessage>(directMessage, this._logger);
                this._logger.Information(requestProjectsMessage, "Processing {actionName} message {@message}", actionName, requestProjectsMessage);

                List<DbeProject> allProjects;

                if (requestProjectsMessage.ProjectSysName != null)
                {
                    allProjects = new List<DbeProject>();

                    var singleProj = await db.Projects.AsNoTracking().SingleOrDefaultAsync(x => x.SysName == requestProjectsMessage.ProjectSysName);
                    if (singleProj != null)
                        allProjects.Add(singleProj);
                }
                else
                    allProjects = await db.Projects.AsNoTracking().ToListAsync();

                var allUsersPack = this._mapper.Map<MainBotProjectInfo[]>(allProjects.ToArray()) ?? new MainBotProjectInfo[0];

                var responseMessage = new MainBotProjectInfoResponseMessage(requestProjectsMessage.SystemEventId) { AllProjectInfos = allUsersPack };


                this._logger.Information(responseMessage, "Response {@response}", responseMessage);
                return JsonSerializer.Serialize(responseMessage);
            }

            await Task.Delay(9000);
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");
            return directMessage;
        }

        private async Task ProcessBuildSystemBuildChanged(BuildServiceBuildChangedMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            this._logger
                .ForContext("message", message, true)
                .Information(message, "Processing ProcessBuildSystemBuildChanged");

            // Ignore delete build. (because I don't want to know that there is less work)
            if (message.NewVersion == null)
                return;

            //Standart build
            //Сборка 341 Проект: СД Планирование(Текущая) Fail
            //
            //Изменения по следующим задачам:
            //(uri) #2708  Убрать проксирующего пользователя
            //
            //Изменения по другим проектам:
            //(uri) #2708 Убрать проксирующего пользователя
            //(uri) #2759 Уход от прокси-пользователя: "выровнять" пользователей в базах ИМ и АСУБС

            //Dump build
            //Сборка 72 Проект: СД Финансирование(Дампы) Success
            //
            //База: aaaaa / sdfutest
            //Дамп от: 19.03.2020(23.36.04)
            //
            //
            //Прочая информация:
            //SdFu Db: sss / sdfutest
            //Fl: file
            //dbV: 93 rcV: 93 currV: 94 isRc: false
            //Inst: C:\Builds\SdFu\93.rc.25

            //Other?


            // Message header. Build info.
            var sb = new StringBuilder(600);
            sb.Append("<b>Сборка <a href='");
            sb.Append(message.BuildUri);
            sb.Append("'>#");
            sb.Append(message.NewVersion.BuildNumber);
            sb.Append("</a>");
            sb.Append("  Проект ");
            sb.Append((message.NewVersion.ProjectSysName ?? message.NewVersion.JobName)?.EscapeHtml());
            sb.Append(" ");
            sb.Append(message.NewVersion.BuildStatus);
            sb.Append("</b> (");
            sb.Append(message.NewVersion.BuildSubType);
            sb.Append(")\n");
            sb.Append(message.NewVersion.ExecuterInfo?.EscapeHtml());
            sb.Append("\n\n");


            if (message.NewVersion.BuildSubType == EnumBuildServerJobs.Dump)
            {
                sb.Append("Дамп от: <b>");
                sb.Append(message.NewVersion.DumpDate?.ToString("dd.MM.yyyy (hh:mm:ss)"));
                sb.Append("</b>\n");

                sb.Append("База данных: <b>");
                sb.Append(message.NewVersion.DumpDb);
                sb.Append("</b>\n\n");

                sb.Append(message.NewVersion.BuildName);
                sb.Append("\n");
                sb.Append(message.NewVersion.BuildDescription);
                sb.Append("\n");
            }
            else if (message.NewVersion.BuildSubType == EnumBuildServerJobs.Current
                     || message.NewVersion.BuildSubType == EnumBuildServerJobs.Rc)
            {

                if (!string.IsNullOrEmpty(message.ArtifactsUri))
                {
                    sb.Append("Сборка: ");
                    sb.Append(message.ArtifactsUri);
                    sb.Append("\n\n");
                }

                var issueNums = message.NewVersion.ChangeInfos?
                    .Select(x => x.IssueId)
                    .Distinct()
                    .Where(x => x != null)
                    .ToArray();

                if (issueNums != null && issueNums.Length > 0)
                {

                    var requestMessageIssue = new BugTrackerTasksRequestMessage(message.SystemEventId)
                    {
                        IssueNums = issueNums
                    };
                    var responseMessageIssue = await this.RabbitService()
                        .DirectRequestTo<BugTrackerTasksRequestMessage, BugTrackerTasksResponseMessage>(
                            EnumInfrastructureServicesType.BugTracker,
                            RabbitMessages.BugTrackerRequestIssues,
                            requestMessageIssue
                        );

                    #region Format function

                    void FormatIssues(string partHeader, Func<BugTrackerIssue, bool> filter)
                    {
                        var filteredIssues = responseMessageIssue.Issues
                            .Where(filter)
                            .ToList();

                        if (filteredIssues.Count > 0)
                        {
                            sb.Append("<b>");
                            sb.Append(partHeader);
                            sb.Append("</b>\n");

                            foreach (var issue in filteredIssues)
                            {
                                sb.Append("<a href=\'");
                                sb.Append(issue.IssueUrl);
                                sb.Append("'>#");
                                sb.Append(issue.Num);
                                sb.Append("</a> ");
                                sb.Append(issue.Subject);
                                sb.Append("\n");
                            }
                        }
                    }

                    #endregion

                    FormatIssues("Изменения по следующим задачам:", iss => iss.ProjectSysName == message.NewVersion.ProjectSysName);
                    sb.Append("\n\n");
                    FormatIssues("Изменения по другим проектам:", iss => iss.ProjectSysName != message.NewVersion.ProjectSysName);
                }

            }
            else if (message.NewVersion.BuildSubType == EnumBuildServerJobs.System ||
                 message.NewVersion.BuildSubType == EnumBuildServerJobs.System)
            {
                sb.Append("\n\n");
                sb.Append(message.NewVersion.BuildSubType);
            }

            // Just for test
            var outgoingMessage = new TelegramOutgoingMessageHtml(message.SystemEventId, sb.ToString())
            {
                BotUserId = _adminId
            };

            await this._rabbitService.PublishInformation(
                RabbitMessages.TelegramOutgoingMessageHtml,
                outgoingMessage);

            //var outgoingMessage = new TelegramOutgoingBuildChangedMessage(message.SystemEventId, "zavjalov");
            //outgoingMessage.BuildUri = message.BuildUri;
            //outgoingMessage.ArtifactsUri = message.ArtifactsUri;
            //outgoingMessage.Text = $"Сборка {message.NewVersion.BuildName} {message.NewVersion.BuildDescription}\n{message.NewVersion.ExecuterInfo}";

            //// Just for test
            //await this._rabbitService.PublishInformation(
            //    RabbitMessages.TelegramOutgoingBuildChanged,
            //    outgoingMessage);


        }

        /// <summary> Process bugtracker issue changed </summary>
        private async Task ProcessBugTrackerIssueChanged(BugTrackerIssueChangedMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            // We receive only "tracked user (has UserBotId)"
            // Now processing logic has next steps:
            // 1. Ignore delete issue. (because I don't want to know that there is less work
            // 2. Send telegram message only to assigned user
            // 3. Ignore if assigned user change his own issue (because I don't want to see message about my own changes
            // 4. Check work time (I don't want to get message at the night) (not realized in nearly future)
            // 5. Have some information about changes.

            this._logger
                .ForContext("message", message, true)
                .Information(message, "Processing ProcessBugTrackerIssueChanged");

            // 1. Ignore delete issue. (because I don't want to know that there is less work)
            if (message.NewVersion == null)
                return;

            // 2. Send telegram message only to assigned user
            if (string.IsNullOrEmpty(message.NewVersion.UserBotIdAssignOn))
            {
                this._logger.Error(message, "Error processing message. UserBotId not found");
                return;
            }

            // 3. Ignore if assigned user change his own issue (because I don't want to see message about my own changes
            if (message.RedmineUserLastChanged != null)
            {
                if (message.RedmineUserLastChanged.All(x => x == message.NewVersion.RedmineAssignOn))
                {
                    this._logger.Information(message, "Ignore message processing. All changed users are the Assigned user '{RedmineAssignOn}'", message.NewVersion.RedmineAssignOn);
                    return;
                }
            }

            // 4. Check work time (I don't want to get message at the night) (not realized in nearly future)
            //..
            //..


            var outgoingMessage = new TelegramOutgoingIssuesChangedMessage(this.GetNextEventId(), message.NewVersion.UserBotIdAssignOn)
            {
                IssueUrl = message.NewVersion.IssueUrl,
                IssueNum = message.NewVersion.Num
            };

            if (message.OldVersion == null)
            {
                outgoingMessage.HeaderText = "Вам новая задача";
                // ReSharper disable once UseStringInterpolation
                outgoingMessage.BodyText = string.Format("{0}\n\n  Проект: {1}\n  Версия: {2}",
                                               message.NewVersion.Subject, 
                                               message.NewVersion.RedmineProjectName, 
                                               message.NewVersion.Version);

            }
            else if (message.OldVersion.RedmineAssignOn != message.NewVersion.RedmineAssignOn)
            {
                outgoingMessage.HeaderText = "Вам назначена задача";
            }
            else if (message.OldVersion.Subject != message.NewVersion.Subject ||
                     message.OldVersion.Description != message.NewVersion.Description)
            {
                outgoingMessage.HeaderText = "Ваша Задача изменена";
            }
            else if (message.HasChanges)
            {
                outgoingMessage.HeaderText = "Ваша Задача изменена";
            }
            else if (message.HasComment)
            {
                outgoingMessage.HeaderText = "В Вашей задаче появился новый комментарий";
            }
            else
            {
                outgoingMessage.HeaderText = "С Вашей задачей что-то случилось";
            }

            if (string.IsNullOrEmpty(outgoingMessage.BodyText))
            {
                // ReSharper disable once UseStringInterpolation
                outgoingMessage.BodyText = string.Format("{0}\n\n  Проект: {1}\n  Версия: {2}\n  Изменяльщик: {3}",
                    message.NewVersion.Subject,
                    message.NewVersion.RedmineProjectName,
                    message.NewVersion.Version,
                    string.Join(", ", message.RedmineUserLastChanged?.Where(x => x != message.NewVersion.RedmineAssignOn)
                                      ?? new List<string>())
                );
            }
            await this._rabbitService.PublishInformation(
                RabbitMessages.TelegramOutgoingIssuesChangedMessage,
                outgoingMessage);
        }

        /// <summary> Process direct request for getting all users </summary>
        /// <param name="message">Empty message (I need only event id)</param>
        /// <param name="rabbitMessageHeaders"></param>
        /// <returns></returns>
        private async Task<ResponseAllUsersMessage> ProcessMainBotDirectGetAllUsers(EmptyMessage message, IDictionary<string, string> rabbitMessageHeaders)
        {
            this._logger.Information(message, "Processing ProcessMainBotDirectGetAllUsers");
            await using var db = this._dbContextFactory.CreateDbContext();

            var allUsers = await db.UsersInfo.AsNoTracking().ToListAsync();
            var allUsersPack = this._mapper.Map<ResponseAllUsersMessage.UserInfo[]>(allUsers.ToArray())
                               ?? new ResponseAllUsersMessage.UserInfo[0];

            var responseMessage = new ResponseAllUsersMessage(message.SystemEventId) { AllUsersInfos = allUsersPack };

            this._logger.Information(responseMessage, "Response {@response}", responseMessage);

            return responseMessage;
        }

        /// <summary> Process a income telegram message. </summary>
        /// <param name="incomeMessage">User message</param>
        /// <param name="rabbitMessageHeaders">Rabbit headers</param>
        private async Task ProcessIncomeTelegramMessage(TelegramIncomeMessage incomeMessage, IDictionary<string, string> rabbitMessageHeaders)
        {
            // Validating user
            this._logger
                .ForContext("incomeMessage", incomeMessage, true)
                .Information(incomeMessage, "Processing message. UserBotId: '{UserBotId}' -- '{incomeMessageText}'",
                    incomeMessage.BotUserId,
                    incomeMessage.MessageText);

            await this.UpdateUserInfoFromTelegram(incomeMessage.BotUserId);
            var isValidUser = await this._telegramProcessor.CheckUser(incomeMessage);
            if (!isValidUser)
            {
                this._logger.Information("User is inactivated. UserBotId '{UserBotId}'", incomeMessage.BotUserId);
                return;
            }

            // Stub? I didn't decide what I want to do with edited messages
            if (incomeMessage.IsEdited)
            {
                this._logger
                    .ForContext("incomeMessage", incomeMessage, true)
                    .Information(incomeMessage, "Message edited. Now ignoring. {incomeMessageText} ", incomeMessage.MessageText);

                return;
            }

            var outgoingPreMessageInfo = this._mapper.Map<OutgoingPreMessageInfo>(incomeMessage);

            var isUnfinishedConversationExist = await this._telegramProcessor.TryToContinueConversation(outgoingPreMessageInfo, incomeMessage);
            if (isUnfinishedConversationExist)
            {
                this._logger
                    .ForContext("incomeMessage", incomeMessage, true)
                    .Information(incomeMessage, "Unfinished conversation found. Message processed. UserBotId '{UserBotId}' '{incomeMessageText}'",
                        incomeMessage.BotUserId,
                        incomeMessage.MessageText);

                return;
            }


            var isNewConversationStarted = await this._telegramProcessor.TryToStartNewCoversation(outgoingPreMessageInfo, incomeMessage);
            if (isNewConversationStarted)
            {
                this._logger
                    .ForContext("incomeMessage", incomeMessage, true)
                    .Information(incomeMessage, "Start new conversation. Message processed. UserBotId '{UserBotId}' '{incomeMessageText}'",
                        incomeMessage.BotUserId,
                        incomeMessage.MessageText);
                return;
            }

            this._logger
                .ForContext("incomeMessage", incomeMessage, true)
                .Warning("Message unprocessed by conversations.");

            await this._rabbitService.PublishInformation(RabbitMessages.TelegramOutgoingMessage,
                new TelegramOutgoingMessage(incomeMessage.SystemEventId, "Not found action " + incomeMessage.MessageText)
                {
                    ChatId = incomeMessage.ChatId
                });

            //var responseMessages = await this._telegramProcessor.ProcessIncomeMessage(incomeMessage).ConfigureAwait(false);

            //if (responseMessages.Count == 0)
            //{
            //    this._logger.Information(incomeMessage, "Nothing to send");
            //    return;
            //}

            //foreach (var outgoingMessage in responseMessages)
            //{
            //    this._logger.Information(outgoingMessage, "Send message through telegram. {@outgoingMessage}", outgoingMessage);
            //    await this._rabbitService.PublishInformation(
            //        RabbitMessages.TelegramOutgoingMessage,
            //        outgoingMessage);
            //}
        }

        #region User data updates

        /// <summary> Update local databse from message if user doesn't exist</summary>
        private async Task UpdateUserInfoFromTelegram(string botUserId)
        {
            await using var db = this._dbContextFactory.CreateDbContext();
            var userExists = db.UsersInfo.Any(x => x.BotUserId == botUserId);
            if (!userExists)
            {
                var userInfo = new DbeUserInfo { BotUserId = botUserId, WhoIsThis = botUserId };
                db.UsersInfo.Add(userInfo);
                await db.SaveChangesAsync();
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
                        usr = new DbeUserInfo
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
                        usr = new DbeUserInfo();

                    usr = this._mapper.Map(newUserInfo, usr);

                    return usr;
                }
                );
        }

        /// <summary> Process user information </summary>
        private async Task ProcessUpdateUser(string findBotId, string eventId, Func<DbeUserInfo?, DbeUserInfo> updateFunc)
        {
            await using var db = this._dbContextFactory.CreateDbContext();

            var user = await db.UsersInfo.FirstOrDefaultAsync(x => x.BotUserId == findBotId);
            if (user != null) // Maybe exists, if telegram message process first
            {
                user = updateFunc(user);

                this._logger.InformationWithEventContext(eventId,
                    "MainBot user exists. Try Update from telegram information {BotUserId}->{@UserInfoNew}",
                    findBotId,
                    user
                );

                db.UsersInfo.Update(user);
                await db.SaveChangesAsync();
            }
            else
            {
                user = updateFunc(null);

                this._logger.InformationWithEventContext(eventId,
                    "MainBot user doesn't exist. New user from telegram information {BotUserId}->{@UserInfoNew}",
                    findBotId,
                    user
                );

                await db.UsersInfo.AddAsync(user);
                await db.SaveChangesAsync();
            }

            // Send user update message to all subscribers
            user = await db.UsersInfo.FirstAsync(x => x.BotUserId == user.BotUserId);
            var updUserMessage = this._mapper.Map<MainBotUpdateUserInfo>(user);
            updUserMessage.SystemEventId = eventId;
            updUserMessage.OriginalBotUserId = findBotId;

            // Publish information for all
            await this._rabbitService.PublishInformation(RabbitMessages.MainBotPublishUpdateUser, updUserMessage);
        }

        #endregion




        public string GetNextEventId()
        {
            return this._eventIdGenerator.GetNextEventId();
        }

        public Task PublishMessage<T>(string actionName, T outgoingMessage, EnumInfrastructureServicesType? subscriberServiceType = null)
            where T : IRabbitMessage
        {
            return this._rabbitService.PublishInformation(actionName, outgoingMessage, subscriberServiceType);
        }

        public async Task<List<DbeProject>> Projects()
        {
            await using var db = this._dbContextFactory.CreateDbContext();
            return await db.Projects.AsNoTracking().ToListAsync();
        }
        
        public async Task<List<BugTrackerIssue>> GetBugTrackerIssues(string projectSysName, string? version)
        {
            var eventId = this._eventIdGenerator.GetNextEventId();

            var requestMessage = new BugTrackerTasksRequestMessage(eventId)
            {
                FilterProjectSysName = projectSysName,
                FilterVersionText = version
            };

            var responseMessage = await this._rabbitService.DirectRequestTo<BugTrackerTasksRequestMessage, BugTrackerTasksResponseMessage>(
                EnumInfrastructureServicesType.BugTracker,
                RabbitMessages.BugTrackerRequestIssues,
                requestMessage
            );

            return responseMessage.Issues.ToList();
        }

        private BotServiceDbContext CreateDbContext()
        {
            return this._dbContextFactory.CreateDbContext();
        }
    }
}