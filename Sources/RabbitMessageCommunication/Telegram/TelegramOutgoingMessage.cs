namespace RabbitMessageCommunication
{
    public class TelegramOutgoingMessage : IRabbitMessage
    {
        public string SystemEventId { get; set; }

        public bool IsEdit { get; set; }

        public long ChatId { get; set; }
        
        public int? MessageId { get; set; }
        
        public string Message { get; set; }
    }
}