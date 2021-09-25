namespace RabbitMessageCommunication.WebAdmin
{
    /// <summary> Response to get-all-user-message </summary>
    public class ResponseAllUsersMessage : IRabbitMessage
    {
        public ResponseAllUsersMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        public string SystemEventId { get; set; }

        public UserInfo[] AllUsersInfos { get; set; }

        /// <summary> Single user info </summary>
        public class UserInfo
        {
            /// <summary> Syntethic user id </summary>
            public int UserId { get; set; }

            /// <summary> User id in bot-system </summary>
            public string? BotUserId { get; set; }

            /// <summary> Is user activated? </summary>
            public bool IsActivate { get; set; }

            /// <summary> User name in bot-system </summary>
            public string? BotUserName { get; set; }

            /// <summary> Autogenerate name in bot-system </summary>
            public string? SystemUserInfo { get; set; }

            /// <summary> Jenkins name </summary>
            public string? JenkinsUserName { get; set; }

            /// <summary> Redmine full name </summary>
            public string? RedmineUserName { get; set; }

        }

    }
}