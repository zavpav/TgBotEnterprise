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
using TelegramService.RabbitCommunication;

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
                var outgoingMessage = new TelegramOutgoingMessageHtml(this._mainBot.GetNextEventId(), "Ничего не найдно")
                {
                    BotUserId = outgoingPreMessageInfo.BotUserId,
                    ChatId = outgoingPreMessageInfo.ChatId
                };

                await this._mainBot.PublishMessage(RabbitMessages.TelegramOutgoingMessageHtml,
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
                if (bugTrackerIssue == null)
                {
                    var outgoingMessageErr = new TelegramOutgoingMessageHtml(this._mainBot.GetNextEventId(), "Ошибка поиска информации по задаче №" + issueNum)
                    {
                        BotUserId = outgoingPreMessageInfo.BotUserId,
                        ChatId = outgoingPreMessageInfo.ChatId,
                    };

                    await this._mainBot.PublishMessage(RabbitMessages.TelegramOutgoingMessageHtml,
                        outgoingMessageErr,
                        EnumInfrastructureServicesType.Messaging);

                    return null;
                }


                var sb = new StringBuilder(600);
                sb.Append("По задаче <a href=\"");
                sb.Append(bugTrackerIssue.IssueUrl);
                sb.Append("\">#");
                sb.Append(responseMessage.IssueNum);
                sb.Append("</a>\n");
                sb.Append("<b>");
                sb.Append(bugTrackerIssue.Subject.EscapeHtml());
                sb.Append("</b>\n");
                sb.Append("Статус: <i>");
                sb.Append(bugTrackerIssue.RedmineStatus);
                sb.Append("</i>\n");


                foreach (var commentsByProject in responseMessage
                    .BuildCommentsInfos
                    .GroupBy(x => new {x.ProjectSysName, x.JenkinsJobName})
                    .OrderBy(x =>
                    {
                        if (x.Key.ProjectSysName == bugTrackerIssue.ProjectSysName)
                            return "1";
                        if (string.IsNullOrEmpty(x.Key.ProjectSysName))
                            return "Z";
                        return "9:" + x.Key.ProjectSysName + ":" + x.Key.JenkinsJobName;
                    })
                    .ThenBy(x => x.Key.JenkinsJobName)
                )
                {
                    sb.Append("\n");
                    sb.Append("Проект: <b>");
                    sb.Append(commentsByProject.Key.ProjectSysName);
                    sb.Append(" - ");
                    sb.Append(commentsByProject.Key.JenkinsJobName);
                    sb.Append("</b>\n");

                    foreach (var commentsByBuild in commentsByProject.GroupBy(x => x.BuildNum)
                        .OrderByDescending(x =>
                        {
                            if (int.TryParse(x.Key, out var issueNumNum))
                                return issueNumNum;

                            return int.MinValue;
                        }))
                    {
                        sb.Append("Сборка <b>#");
                        sb.Append(commentsByBuild.Key);
                        sb.Append("</b>\n");

                        foreach (var cmm in commentsByBuild)
                        {
                            sb.Append("  ");
                            sb.Append(cmm.GitComment?.Trim());
                            sb.Append("\n");
                        }
                    }
                }

                var outgoingMessage = new TelegramOutgoingMessageHtml(this._mainBot.GetNextEventId(), sb.ToString())
                {
                    BotUserId = outgoingPreMessageInfo.BotUserId,
                    ChatId = outgoingPreMessageInfo.ChatId,
                };

                await this._mainBot.PublishMessage(RabbitMessages.TelegramOutgoingMessageHtml,
                    outgoingMessage,
                    EnumInfrastructureServicesType.Messaging);
            }



            return null;
        }

    }
}