namespace RabbitMessageCommunication.BugTracker
{
    /// <summary> Response information about issues from BugTracker </summary>
    public class BugTrackerTasksResponseMessage : IRabbitMessage
    {
        public BugTrackerTasksResponseMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Found issues </summary>
        public BugTrackerIssue[] Issues { get; set; } = new BugTrackerIssue[0];
    }
}