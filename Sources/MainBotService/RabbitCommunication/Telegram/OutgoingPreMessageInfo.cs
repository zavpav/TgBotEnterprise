namespace MainBotService.RabbitCommunication.Telegram
{
    /// <summary> Some information about generating output telegram message </summary>
    public class OutgoingPreMessageInfo
    {
        public OutgoingPreMessageInfo(string botUserId)
        {
            this.BotUserId = botUserId;
        }

        /// <summary> User id in bot-system </summary>
        public string BotUserId { get; set; }

        /// <summary> Telegram chatId (if we know) </summary>
        public long? ChatId { get; set; }

        /// <summary> Telegram messageId (if we know) </summary>
        public int? MessageId { get; set; }

        /// <summary> BotMessageId (if we know) </summary>
        public string? BotMessageId { get; set; }
    }
}