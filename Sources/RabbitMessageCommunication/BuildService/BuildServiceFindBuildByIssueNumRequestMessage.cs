namespace RabbitMessageCommunication.BuildService
{
    /// <summary> Get build information by Issue Num </summary>
    public class BuildServiceFindBuildByIssueNumRequestMessage : IRabbitMessage
    {
        public BuildServiceFindBuildByIssueNumRequestMessage(string systemEventId, string issueNum)
        {
            this.SystemEventId = systemEventId;
            this.IssueNum = issueNum;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Issue number </summary>
        public string IssueNum { get; set; }
    }
}