namespace RabbitMessageCommunication.WebAdmin
{
    public class WebAdminUpdateUserInfo : IRabbitMessage
    {
        public WebAdminUpdateUserInfo(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; set; }

        /// <summary> Current user id in bot-system </summary>
        public string BotUserId { get; set; }

        /// <summary> Old user id in bot-system (if botUserId is changed) </summary>
        public string? OriginalBotUserId { get; set; }

        /// <summary> Information about user </summary>
        public string? WhoIsThis { get; set; }

        /// <summary> Is User active? </summary>
        public bool IsActive { get; set; } = false;

        /// <summary> Jenkins name </summary>
        public string? JenkinsUserName { get; set; }

        /// <summary> Redmine full name </summary>
        public string? RedmineUserName { get; set; }

    }
}