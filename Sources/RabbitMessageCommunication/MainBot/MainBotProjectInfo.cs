namespace RabbitMessageCommunication.MainBot
{
    /// <summary> Information about project </summary>
    public class MainBotProjectInfo
    {
        /// <summary> System name of project </summary>
        public string SysName { get; set; }

        /// <summary> Name for people </summary>
        public string Description { get; set; }

        /// <summary> Current version Number (as in Redmine) </summary>
        public string? CurrentVersion { get; set; }

        /// <summary> Rc version Number (as in Redmine) </summary>
        public string? RcVersion { get; set; }
    }
}