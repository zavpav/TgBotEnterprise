using System.Collections.Generic;
using RabbitMessageCommunication.BugTracker;

namespace MainBotService.RabbitCommunication
{
    /// <summary> Storage class </summary>
    public class DtoBugTrackerIssues
    {
        /// <summary> Prefix for bugtracker server </summary>
        public string HttpIssuePrefix { get; set; } = "";

        /// <summary> Issues </summary>
        public List<BugTrackerIssue> Issues { get; set; } = new List<BugTrackerIssue>();
    }

}