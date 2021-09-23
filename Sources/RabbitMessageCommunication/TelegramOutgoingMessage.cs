namespace RabbitMessageCommunication
{
    public class TelegramOutgoingMessage
    {
        /// <summary> Message Income Id - Unique id in all services </summary>
        public string IncomeId { get; set; }

        public bool IsEdit { get; set; }

        public long ChatId { get; set; }
        public int? MessageId { get; set; }
        public string Message { get; set; }
    }
}