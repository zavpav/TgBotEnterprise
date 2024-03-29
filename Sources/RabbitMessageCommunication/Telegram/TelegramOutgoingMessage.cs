﻿namespace RabbitMessageCommunication
{
    public class TelegramOutgoingMessageHtml : IRabbitMessage
    {
        public TelegramOutgoingMessageHtml(
            string systemEventId,
            string messageHtml)
        {
            this.SystemEventId = systemEventId;
            this.MessageHtml = messageHtml;
        }

        public string SystemEventId { get; set; }

        /// <summary> User id in bot-system </summary>
        public string? BotUserId { get; set; }

        public bool IsEdit { get; set; }

        public long? ChatId { get; set; }
        
        public int? MessageId { get; set; }
        
        /// <summary> Html formatted message for telegram </summary>
        public string MessageHtml { get; set; }
    }
}