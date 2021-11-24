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

        public Tuple<string, string>[]? BuildCommentsInfo { get; set; }

    }
}