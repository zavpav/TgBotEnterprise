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

        /// <summary> Service has a big problem </summary>
        public const string ServiceProblem = "ServiceProblem";

        #region Messages for/from telegram

        /// <summary> Telegram service received a message from user </summary>
        public const string TelegramMessageReceived = "TelegramIncomeMessage";

        /// <summary> Telegram service must send a message </summary>
        public const string TelegramOutgoingMessage = "TelegramOutgoingMessage";

        /// <summary> Telegram service must send a html formatted message </summary>
        public const string TelegramOutgoingMessageHtml = "TelegramOutgoingMessageHtml";

        /// <summary> Telegram service must send information about bugtracker issues </summary>
        public const string TelegramOutgoingIssuesMessage = "TelegramOutgoingIssuesMessage";

        /// <summary> Telegram service must send information about changed bugtracker issues </summary>
        public const string TelegramOutgoingIssuesChangedMessage = "TelegramOutgoingIssuesChangedMessage";

        /// <summary> Telegram service must send information about changed build infromation </summary>
        public const string TelegramOutgoingBuildChanged = "TelegramOutgoingBuildChanged";

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

        /// <summary> Information about changed issue </summary>
        public const string BugTrackerIssueChanged = "BugTrackerIssueChanged";

        #endregion

        #region BuildSystem

        /// <summary> Information about changed build </summary>
        public const string BuildSystemBuildChanged = "BuildSystemBuildChanged";


        /// <summary> Request information from build service by issue number </summary>
        public const string BuildServiceFindBuildByIssueNum = "BuildServiceFindBuildByIssueNum";

        #endregion

    }
}
