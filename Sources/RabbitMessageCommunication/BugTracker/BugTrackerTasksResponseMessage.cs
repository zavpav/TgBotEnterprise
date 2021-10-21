﻿using System;

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

        
        /// <summary> BugTracker issue </summary>
        public class BugTrackerIssue
        {
            /// <summary> Issue num </summary>
            public string Num { get; set; } = "";

            /// <summary> Issue subject </summary>
            public string Subject { get; set; } = "";

            /// <summary> Issue text </summary>
            public string Description { get; set; } = "";

            /// <summary> Issue status </summary>
            public string Status { get; set; } = "";

            /// <summary> Version </summary>
            public string Version { get; set; } = "";

            /// <summary> Resolution </summary>
            public string Resolution { get; set; } = "";

            /// <summary> Project name from bugtracker </summary>
            public string ProjectName { get; set; } = "";

            /// <summary> Project sysname </summary>
            public string ProjectSysName { get; set; } = "";

            /// <summary> Last update date-time </summary>
            public DateTime UpdateOn { get; set; }

            /// <summary> BotSystemUserId  </summary>
            public string? AssignOnUserBotId { get; set; } = "";

            /// <summary> Whom assign issue (User name from bugtracker system) </summary>
            public string CreatorName { get; set; } = "";

            /// <summary> Whom assign issue (User name from bugtracker system) </summary>
            public string AssignOn { get; set; } = "";
        }
    }
}