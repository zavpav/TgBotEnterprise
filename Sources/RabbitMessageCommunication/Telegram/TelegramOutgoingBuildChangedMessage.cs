namespace RabbitMessageCommunication
{
    public class TelegramOutgoingBuildChangedMessage : IRabbitMessage
    {
        public TelegramOutgoingBuildChangedMessage(string systemEventId, string botUserId)
        {
            this.SystemEventId = systemEventId;
            this.BotUserId = botUserId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> User id in bot-system </summary>
        public string BotUserId { get; }

        /// <summary> Http address for opening information about build </summary>
        public string? BuildUri { get; set; }

        /// <summary> Uri for build artifacts (binary files etc) </summary>
        public string? ArtifactsUri { get; set; }

        /// <summary> Dummy text </summary>
        public string? Text { get; set; }
    }
}