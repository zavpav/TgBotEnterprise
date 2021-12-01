using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication;
using RabbitMessageCommunication.BugTracker;
using RabbitMessageCommunication.BuildService;
using RabbitMqInfrastructure;
using Serilog;

namespace MainBotService.RabbitCommunication.Telegram
{
    /// <summary> Information from build system about task. Which builds and so on </summary>
    public class TaskBuildInformationConversation : ITelegramConversation
    {
        private readonly IMainBotService _mainBot;
        private readonly ILogger _logger;

        private Regex ReFirstMessage { get; }

        public TaskBuildInformationConversation(IMainBotService mainBot, ILogger logger)
        {
            this._mainBot = mainBot;
            this._logger = logger;
            this.ReFirstMessage = new Regex(@"^(сборки по задаче|builds by task)\s+#?(?<issnum>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public Task<bool> IsStartingMessage(string messageText)
        {
            var isConversation = this.ReFirstMessage.IsMatch(messageText);
            return Task.FromResult(isConversation);
        }

        public async Task<string?> NextConversationStep(string step, OutgoingPreMessageInfo outgoingPreMessageInfo, string messageText)
        {
            this._logger.Information("TaskBuildInformationConversation {step}", step);

            var eventId = this._mainBot.GetNextEventId();
            var mch = this.ReFirstMessage.Match(messageText);
            if (!mch.Success)
            {
                this._logger.Error("Error find issue number {messageText}", messageText);
                return null;
            }

            var issueNum = this.ReFirstMessage.Match(messageText).Groups["issnum"].Value;
            var requestMessage = new BuildServiceFindBuildByIssueNumRequestMessage(eventId, issueNum);


            var responseMessage = await this._mainBot.RabbitService()
                .DirectRequestTo<BuildServiceFindBuildByIssueNumRequestMessage, BuildServiceFindBuildByIssueNumResponseMessage>(
                EnumInfrastructureServicesType.BuildService,
                RabbitMessages.BuildServiceFindBuildByIssueNum,
                requestMessage
            );

            if (responseMessage.BuildCommentsInfos == null)
            {
                var outgoingMessage = new TelegramOutgoingMessage(this._mainBot.GetNextEventId())
                {
                    BotUserId = outgoingPreMessageInfo.BotUserId,
                    ChatId = outgoingPreMessageInfo.ChatId,
                    Message = "Ничего не найдно"
                };

                await this._mainBot.PublishMessage(RabbitMessages.TelegramOutgoingMessage,
                    outgoingMessage,
                    EnumInfrastructureServicesType.Messaging);
            }
            else
            {
                var requestMessageIssue = new BugTrackerTasksRequestMessage(eventId)
                {
                    IssueNums = new []{ issueNum }
                };
                var responseMessageIssue = await this._mainBot.RabbitService()
                    .DirectRequestTo<BugTrackerTasksRequestMessage, BugTrackerTasksResponseMessage>(
                        EnumInfrastructureServicesType.BugTracker,
                        RabbitMessages.BugTrackerRequestIssues,
                        requestMessageIssue
                    );

                if (responseMessageIssue.Issues?.Length > 1)
                    this._logger
                        .ForContext("IssueInfo", responseMessageIssue, true)
                        .Error("Some strange. Bug tracker return too many issues");

                var bugTrackerIssue = responseMessageIssue.Issues?[0];

                var sb = new StringBuilder(100);
                sb.Append("По задаче #");
                sb.Append(responseMessage.IssueNum);
                sb.Append("\n");
                sb.Append(bugTrackerIssue?.Subject ?? "<Информация по задаче не найдена>");
                sb.Append("\n");
                sb.Append("Статус: ");
                sb.Append(bugTrackerIssue?.RedmineStatus ?? "<Не знаю>");
                sb.Append("\n");


                foreach (var commentsByProject in responseMessage
                    .BuildCommentsInfos
                    .GroupBy(x => x.ProjectSysName)
                    .OrderBy(x =>
                    {
                        if (x.Key == bugTrackerIssue.ProjectSysName)
                            return "1";
                        if (string.IsNullOrEmpty(x.Key))
                            return "Z";
                        return "9" + x.Key;
                    })
                )
                {
                    sb.Append("\n");
                    sb.Append("Проект ");
                    sb.Append(commentsByProject.Key);

                    foreach (var commentsByBuild in commentsByProject.GroupBy(x => x.BuildNum)
                        .OrderByDescending(x =>
                        {
                            if (int.TryParse(x.Key, out var issueNumNum))
                                return issueNumNum;

                            return int.MinValue;
                        }))
                    {

                        sb.Append("\n");
                        sb.Append("Сборка #");
                        sb.Append(commentsByBuild.Key);
                        sb.Append("\n");

                        foreach (var cmm in commentsByBuild)
                        {
                            sb.Append("  ");
                            sb.Append(cmm.GitComment?.Trim());
                            sb.Append("\n");
                        }
                    }
                }

                var outgoingMessage = new TelegramOutgoingMessage(this._mainBot.GetNextEventId())
                {
                    BotUserId = outgoingPreMessageInfo.BotUserId,
                    ChatId = outgoingPreMessageInfo.ChatId,
                    Message = sb.ToString()
                };

                await this._mainBot.PublishMessage(RabbitMessages.TelegramOutgoingMessage,
                    outgoingMessage,
                    EnumInfrastructureServicesType.Messaging);
            }



            return null;
        }

    }
}