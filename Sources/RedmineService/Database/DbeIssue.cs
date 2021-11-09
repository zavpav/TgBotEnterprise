using System;
using RabbitMessageCommunication.BugTracker;

namespace RedmineService.Database
{
    public class DbeIssue
    {
        /// <summary> Synthetic id </summary>
        public int Id { get; set; }

        /// <summary> Issue num </summary>
        public string Num { get; set; } = "";

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

        /// <summary> Priority from redmine </summary>
        public string RedminePriority { get; set; } = "";

        /// <summary> Project name from bugtracker </summary>
        public string RedmineProjectName { get; set; } = "";

        /// <summary> Project sysname from bot system </summary>
        public string? ProjectSysName { get; set; }

        /// <summary> Creation date-time </summary>
        public DateTime CreateOn { get; set; }

        /// <summary> Last update date-time </summary>
        public DateTime UpdateOn { get; set; }

        /// <summary> BotSystemUserId Whom assign issue  </summary>
        public string? UserBotIdAssignOn { get; set; }

        /// <summary> Whom assign issue (User name from bugtracker system) </summary>
        public string RedmineAssignOn { get; set; } = "";

        /// <summary> Whom assign issue (User name from bugtracker system) </summary>
        public string CreatorName { get; set; } = "";

    }
}