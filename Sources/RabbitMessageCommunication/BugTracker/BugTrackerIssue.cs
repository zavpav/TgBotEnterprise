using System;

namespace RabbitMessageCommunication.BugTracker
{
    /// <summary> BugTracker issue </summary>
    public class BugTrackerIssue
    {
        /// <summary> Issue num </summary>
        public string Num { get; set; } = "";

        /// <summary> Url for open issue in browser </summary>
        public string IssueUrl { get; set; } = "";

        /// <summary> Issue subject </summary>
        public string Subject { get; set; } = "";

        /// <summary> Issue text </summary>
        public string Description { get; set; } = "";

        /// <summary> Resolution </summary>
        public string Resolution { get; set; } = "";

        /// <summary> Issue status from bugtracker </summary>
        public string RedmineStatus { get; set; } = "";

        /// <summary> Logic status of issue </summary>
        public EnumIssueStatus IssueStatus { get; set; } = EnumIssueStatus.NotDefined;

        /// <summary> Version </summary>
        public string Version { get; set; } = "";

        /// <summary> Project name from bugtracker </summary>
        public string RedmineProjectName { get; set; } = "";

        /// <summary> Project sysname </summary>
        public string ProjectSysName { get; set; } = "";

        /// <summary> Last update date-time </summary>
        public DateTime UpdateOn { get; set; }

        /// <summary> BotSystemUserId  </summary>
        public string? UserBotIdAssignOn { get; set; }

        /// <summary> Whom assign issue (User name from bugtracker system) </summary>
        public string RedmineAssignOn { get; set; } = "";

        /// <summary> Whom assign issue (User name from bugtracker system) </summary>
        public string CreatorName { get; set; } = "";

        /// <summary> Sorting (if needed) </summary>
        public int OrderBy { get; set; }
    }
}