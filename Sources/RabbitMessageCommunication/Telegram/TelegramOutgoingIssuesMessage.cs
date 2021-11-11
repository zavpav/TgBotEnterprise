using RabbitMessageCommunication.BugTracker;

namespace RabbitMessageCommunication
{
    public class TelegramOutgoingIssuesMessage : IRabbitMessage
    {
        public TelegramOutgoingIssuesMessage(string systemEventId, string botUserId)
        {
            this.SystemEventId = systemEventId;
            this.BotUserId = botUserId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> User id in bot-system </summary>
        public string BotUserId { get; }

        /// <summary> Chat Id from telegram </summary>
        public long? ChatId { get; set; }

        /// <summary> Http prefix of redmine for open issue in browser </summary>
        public string IssueHttpFullPrefix { get; set; } = "";

        /// <summary> Found issues </summary>
        public BugTrackerIssue[] Issues { get; set; } = new BugTrackerIssue[0];
    }
}