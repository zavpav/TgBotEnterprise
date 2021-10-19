namespace RabbitMessageCommunication
{
    /// <summary> Empty message. Doesn't have any data (except eventId) </summary>
    public class EmptyMessage : IRabbitMessage
    {
        public EmptyMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Message Income Id - Unique id in all services </summary>
        public string SystemEventId { get; set; }

    }
}