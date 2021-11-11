namespace RabbitMessageCommunication.BugTracker
{
    /// <summary> Information about changed issues from redmine </summary>
    public class BugTrackerIssueChangedMessage : IRabbitMessage
    {
        public BugTrackerIssueChangedMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Http prefix of redmine for open issue in browser </summary>
        public string IssueHttpFullPrefix { get; set; } = "";

        /// <summary> Old version of issue. null - for new Version of issue </summary>
        public BugTrackerIssue? OldVersion { get; set; }

        /// <summary> New version of issue. null - for deleted issue </summary>
        public BugTrackerIssue? NewVersion { get; set; }

        /// <summary> Has new comments </summary>
        public bool HasComment { get; set; }

        /// <summary> Has any changes </summary>
        public bool HasChanges { get; set; }

        /// <summary> Last changed users </summary>
        public string[]? RedmineUserLastChanged { get; set; }

    }
}