namespace RabbitMessageCommunication.MainBot
{
    /// <summary> Request main infromation about projects </summary>
    public class MainBotProjectInfoRequestMessage : IRabbitMessage
    {
        public MainBotProjectInfoRequestMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }
        
        /// <summary> Project sys name </summary>
        /// <remarks>
        /// if it's empty means need all information </remarks>
        public string? ProjectSysName { get; set; }
    }
}