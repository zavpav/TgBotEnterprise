namespace RabbitMessageCommunication
{
    public class TelegramOutgoingMessage
    {
        public bool IsEdit { get; set; }

        public long ChatId { get; set; }
        public int? MessageId { get; set; }
        public string Message { get; set; }
    }
}