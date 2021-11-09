namespace RabbitMessageCommunication.WebAdmin
{
    /// <summary> Response to get-all-user-message </summary>
    public class ResponseAllUsersMessage : IRabbitMessage
    {
        public ResponseAllUsersMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Message Income Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        public UserInfo[] AllUsersInfos { get; set; } = new UserInfo[0];

        /// <summary> Single user info </summary>
        public class UserInfo
        {
            /// <summary> Syntethic user id </summary>
            public int UserId { get; set; }

            /// <summary> User id in bot-system </summary>
            public string? BotUserId { get; set; }

            /// <summary> Is user activated? </summary>
            public bool IsActive { get; set; }

            /// <summary> User name in bot-system </summary>
            public string? WhoIsThis { get; set; }

            /// <summary> Jenkins name </summary>
            public string? JenkinsUserName { get; set; }

            /// <summary> Redmine full name </summary>
            public string? RedmineUserName { get; set; }

        }

    }
}