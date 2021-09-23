namespace RabbitMessageCommunication
{
    /// <summary> Income telegram message </summary>
    public class TelegramIncomeMessage
    {
        /// <summary> Message Income Id - Unique id in all services </summary>
        public string IncomeId { get; set; }

        /// <summary> Internal Id from telegram </summary>
        public int UpdateId { get; set; }

        /// <summary> Message Id from telegram </summary>
        public int MessageId { get; set; }
        
        /// <summary> Chat Id from telegram </summary>
        public long ChatId { get; set; }

        /// <summary> Is it direct message to bot? </summary>
        /// <remarks> false - if message from group chat</remarks>
        public bool IsDirectMessage { get; set; }

        /// <summary> User Id from telegram </summary>
        public long TelegramUserId { get; set; }

        /// <summary> User id in bot-system </summary>
        public string BotUserId { get; set; }

        /// <summary> MessageText Text </summary>
        public string MessageText { get; set; }
        
        /// <summary> Is the message edited? </summary>
        public bool IsEdited { get; set; }

        
    }
}