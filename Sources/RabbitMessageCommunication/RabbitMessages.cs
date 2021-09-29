using System;

namespace RabbitMessageCommunication
{
    /// <summary> List of rabbit messages </summary>
    /// <remarks>
    ///  In "microservices" this class doesnt need. But it reduce typos.
    /// </remarks>
    public static class RabbitMessages
    {
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
        public const string MainBotPublishUpdateUser = "MainBotPublishNewUser";

        #endregion

        #region WebAdmin

        /// <summary> Publish information about update user information </summary>
        public const string WebGetAllUsers = "MainBotPublishNewUser";

        #endregion

    }
}
