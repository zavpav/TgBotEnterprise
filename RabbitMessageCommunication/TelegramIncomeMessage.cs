namespace RabbitMessageCommunication
{
    public class TelegramIncomeMessage
    {
        public int UpdateId { get; set; }

        public int MessageId { get; set; }
        
        public long ChatId { get; set; }
        
        public long TelegramUserId { get; set; }
        
        public string Message { get; set; }
        
        public bool IsEdited { get; set; }
    }
}