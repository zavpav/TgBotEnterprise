using System;

namespace RabbitMessageCommunication
{
    /// <summary> List of rabbit messages </summary>
    /// <remarks>
    ///  In "microservices" this class doesnt need. But it reduce typos.
    /// </remarks>
    public static class RabbitMessages
    {
        /// <summary> Telegram service received a message from user </summary>
        public const string TelegramMessageReceived = "TG_INCOME_MESSAGE";

        /// <summary> Telegram service must send a message </summary>
        public const string TelegramOutgoingMessage = "TG_OUTGOING_MESSAGE";
    }
}
