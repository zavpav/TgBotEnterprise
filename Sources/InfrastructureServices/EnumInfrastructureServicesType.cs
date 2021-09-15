namespace CommonInfrastructure
{
    /// <summary> Types of Nodes in TgBotSystem </summary>
    public enum EnumInfrastructureServicesType
    {
        /// <summary> Main bot </summary>
        Main,
        
        /// <summary> Bugtracking service </summary>
        BugTracker,
        
        /// <summary> CI-buind service </summary>
        BuildService,

        /// <summary> Messaging service </summary>
        Messaging,

        /// <summary> Git service </summary>
        Git,

        /// <summary> Web admin </summary>
        WebAdmin
    }
}