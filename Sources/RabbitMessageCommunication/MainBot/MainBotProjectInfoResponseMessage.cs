namespace RabbitMessageCommunication.MainBot
{
    /// <summary> Simple information from main bot about projects </summary>
    /// <remarks>
    ///  Information from only main bot. Exclude redmine/jenkins and other information.
    /// </remarks>
    public class MainBotProjectInfoResponseMessage : IRabbitMessage
    {
        public MainBotProjectInfoResponseMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; set; }

        /// <summary> All projects information  </summary>
        public MainBotProjectInfo[] AllProjectInfos { get; set; } = new MainBotProjectInfo[0];
    }
}