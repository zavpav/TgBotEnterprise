using System;

namespace RabbitMessageCommunication
{
    /// <summary> List of rabbit messages </summary>
    /// <remarks>
    ///  In "microservices" this class doesnt need. But it reduce typos.
    /// </remarks>
    public static class RabbitMessages
    {
        /// <summary> Simple ping message </summary>
        public const string PingMessage = "PING";

        #region Messages for/from telegram

        /// <summary> Telegram service received a message from user </summary>
        public const string TelegramMessageReceived = "TelegramIncomeMessage";

        /// <summary> Telegram service must send a message </summary>
        public const string TelegramOutgoingMessage = "TelegramOutgoingMessage";

        /// <summary> Publish information from telegram about new user </summary>
        public const string TelegramPublishNewUserFromTelegram = "TelegramPublishNewUserFromTelegram";

        #endregion

        #region Messages from MainBot

        /// <summary> Publish information about update user information </summary>
        public const string MainBotPublishUpdateUser = "MainBotPublishUpdateUser";

        /// <summary> Direct request information from main bot about all users </summary>
        public const string MainBotDirectGetAllUsers = "MainBotDirectGetAllUsers ";

        /// <summary> Direct request information from main bot about all exiting projects </summary>
        public const string MainBotProjectsInfoRequest = "MainBotProjectsInfoRequest";

        /// <summary> Publish project innformation changes  </summary>
        public const string MainBotProjectSettingsUpdate = "MainBotProjectSettingsUpdate";

        #endregion

        #region WebAdmin

        /// <summary> Publish information about update user information </summary>
        public const string WebAdminPublishUpdateUser = "WebAdminPublishUpdateUser";

        /// <summary> Request needed project settings from other services </summary>
        public const string WebAdminProjectSettingsRequest = "WebAdminProjectSettingsRequest";

        /// <summary> Response on request needed project settings from other services </summary>
        public const string WebAdminProjectSettingsResponse = "WebAdminProjectSettingsResponse";

        /// <summary> Update needed project settings on each service </summary>
        public const string WebAdminProjectSettingsUpdate = "WebAdminProjectSettingsUpdate";

        #endregion

        #region BugTracker

        /// <summary> Request issues from BugTracker </summary>
        public const string BugTrackerRequestIssues = "BugTrackerRequestIssues";

        #endregion

    }
}
