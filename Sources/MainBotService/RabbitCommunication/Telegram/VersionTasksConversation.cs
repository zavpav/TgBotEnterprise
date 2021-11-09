using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication;
using Serilog;

namespace MainBotService.RabbitCommunication.Telegram
{
    public class VersionTasksConversation : ITelegramConversation
    {
        private readonly IMainBotService _mainBot;
        private readonly ILogger _logger;
        private Regex ReFirstMessage { get; }

        public VersionTasksConversation(IMainBotService mainBot, ILogger logger)
        {
            this._mainBot = mainBot;
            this._logger = logger;
            this.ReFirstMessage = new Regex(@"^(версия|version)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public Task<bool> IsStartingMessage(string messageText)
        {
            var isConversation = this.ReFirstMessage.IsMatch(messageText);
            return Task.FromResult(isConversation);
        }

        public async Task<string?> NextConversationStep(string step, 
            OutgoingPreMessageInfo outgoingPreMessageInfo,
            string messageText)
        {
            var projectVersions = await this._mainBot.Projects();

            foreach (var project in projectVersions)
            {
                if (string.IsNullOrEmpty(project.CurrentVersion))
                    continue;

                var issues = await this._mainBot.GetBugTrackerIssues(project.SysName, project.CurrentVersion);
                if (issues.Count == 0)
                {
                    this._logger.Information("No issues found {ProjectSysName} {Version}", 
                        project.SysName, 
                        project.CurrentVersion);
                }
                else
                {
                    this._logger.Information("Found issues {ProjectSysName} {Version} : {Count}",
                        project.SysName,
                        project.CurrentVersion,
                        issues.Count);

                    // Final formatting must be in telegram service because TelegramMessage has restrict for length and html formatting
                    // that's why send message with all issues to telegramService
                    var outgoingMessage = new TelegramOutgoingIssuesMessage(this._mainBot.GetNextEventId(), outgoingPreMessageInfo.BotUserId)
                    {
                        ChatId = outgoingPreMessageInfo.ChatId,
                        Issues = issues.ToArray()
                    };

                    await this._mainBot.PublishMessage(RabbitMessages.TelegramOutgoingIssuesMessage, 
                            outgoingMessage,
                            EnumInfrastructureServicesType.Messaging);
                }
            }

            this._logger.Information("VersionTasksConversation");
            return null;
        }
    }
}