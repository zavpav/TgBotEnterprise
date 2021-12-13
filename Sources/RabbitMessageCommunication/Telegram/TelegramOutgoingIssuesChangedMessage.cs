namespace RabbitMessageCommunication
{
    /// <summary> Message about changing issue in bugtracker </summary>
    public class TelegramOutgoingIssuesChangedMessage : IRabbitMessage
    {
        public TelegramOutgoingIssuesChangedMessage(string systemEventId, string botUserId)
        {
            this.SystemEventId = systemEventId;
            this.BotUserId = botUserId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> User id in bot-system </summary>
        public string BotUserId { get; }

        /// <summary> Header of message text </summary>
        public string HeaderText { get; set; } = "";

        /// <summary> Http prefix of redmine for open issue in browser </summary>
        public string IssueUrl { get; set; } = "";

        /// <summary> Number of changed issue </summary>
        public string IssueNum { get; set; } = "";

        /// <summary> Main text </summary>
        public string BodyText { get; set; } = "";
    }
}