namespace RabbitMessageCommunication.BuildService
{
    /// <summary> Information about changed build from buildservice </summary>
    public class BuildServiceBuildChangedMessage : IRabbitMessage
    {
        public BuildServiceBuildChangedMessage(string systemEventId)
        {
            this.SystemEventId = systemEventId;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Http address for opening information about build </summary>
        public string? BuildUri { get; set; }

        /// <summary> Uri for build artifacts (binary files etc) </summary>
        public string? ArtifactsUri { get; set; }

        /// <summary> Old infromation of build. null - for new Version of issue </summary>
        public BuildInfo? OldVersion { get; set; }

        /// <summary> New version of issue. null - for deleted issue </summary>
        public BuildInfo? NewVersion { get; set; }

    }
}