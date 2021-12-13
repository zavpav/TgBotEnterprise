namespace RabbitMessageCommunication
{
    public class TelegramOutgoingMessage : IRabbitMessage
    {
        public TelegramOutgoingMessage(string systemEventId, string message)
        {
            this.SystemEventId = systemEventId;
            this.Message = message;
        }

        public string SystemEventId { get; set; }

        /// <summary> User id in bot-system </summary>
        public string? BotUserId { get; set; }

        public bool IsEdit { get; set; }

        public long? ChatId { get; set; }
        
        public int? MessageId { get; set; }
        
        /// <summary> Plane message for telegram </summary>
        public string Message { get; set; }
    }
}