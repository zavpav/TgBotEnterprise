namespace RabbitMessageCommunication.WebAdmin
{
    /// <summary> Request all users from mainbot </summary>
    public class RequestAllUsersMessage : IRabbitMessage
    {
        public RequestAllUsersMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        public string SystemEventId { get; set; }
    }
}