namespace RabbitMessageCommunication.MainBot
{
    /// <summary> Update project information main bot </summary>
    /// <remarks>
    ///  Information from only main bot. Exclude redmine/jenkins and other information.
    /// </remarks>
    public class MainBotProjectInfoUpdateMessage : IRabbitMessage
    {
        public MainBotProjectInfoUpdateMessage(string systemEventId, MainBotProjectInfo projectInfo)
        {
            this.SystemEventId = systemEventId;
            this.ProjectInfo = projectInfo;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Project information  </summary>
        public MainBotProjectInfo ProjectInfo { get; }
    }
}