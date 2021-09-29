namespace RabbitMessageCommunication
{
    public class TelegramPublishNewUserFromTelegram : IRabbitMessage
    {
        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; set; }

        /// <summary> User id in bot-system </summary>
        public string BotUserId { get; set; }

        /// <summary> Information about user </summary>
        public string? WhoIsThis { get; set; }

        /// <summary> Is User active? </summary>
        public bool IsActive { get; set; }
    }
}