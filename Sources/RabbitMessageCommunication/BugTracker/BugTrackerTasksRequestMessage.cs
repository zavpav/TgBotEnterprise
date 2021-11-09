namespace RabbitMessageCommunication.BugTracker
{
    /// <summary> Get information about issues from BugTracker </summary>
    public class BugTrackerTasksRequestMessage : IRabbitMessage
    {
        public BugTrackerTasksRequestMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Request tasks by project </summary>
        public string? FilterProjectSysName { get; set; }

        /// <summary> Request tasks for user </summary>
        public string? FilterUserBotId { get; set; }
        
        /// <summary> Request tasks by version </summary>
        public string? FilterVersionText { get; set; }
    }
}