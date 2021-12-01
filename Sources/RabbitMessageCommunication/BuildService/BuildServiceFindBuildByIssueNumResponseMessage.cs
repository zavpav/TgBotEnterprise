using System;

namespace RabbitMessageCommunication.BuildService
{
    public class BuildServiceFindBuildByIssueNumResponseMessage : IRabbitMessage
    {
        public BuildServiceFindBuildByIssueNumResponseMessage(string systemEventId, string issueNum)
        {
            this.SystemEventId = systemEventId;
            this.IssueNum = issueNum;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Issue number </summary>
        public string IssueNum { get; set; }

        /// <summary> Comments from builds </summary>
        public BuildCommentsInfo[]? BuildCommentsInfos { get; set; }

        public class BuildCommentsInfo
        {
            /// <summary> Project sysname </summary>
            public string? ProjectSysName { get; set; } = "";

            /// <summary> Name of job from Jenkins </summary>
            public string? JenkinsJobName { get; set; }

            /// <summary> Number of build </summary>
            public string? BuildNum { get; set; }

            /// <summary> Comment from git </summary>
            public string? GitComment { get; set; }
        }

    }
}