namespace RabbitMessageCommunication.BugTracker
{
    /// <summary> Issue statuses </summary>
    public enum EnumIssueStatus
    {
        /// <summary> Not defined </summary>
        NotDefined,
        
        /// <summary> Draft </summary>
        /// <returns>Haven't needed realization yet</returns>
        Draft,

        /// <summary> Ready </summary>
        /// <remarks>Ready for realization (or other working)</remarks>
        Ready,

        /// <summary> Developing </summary>
        /// <remarks>Currently processing</remarks>
        Developing,

        /// <summary> Realization finished but not tested </summary>
        Resolved,

        /// <summary> On testing </summary>
        OnTesting,

        /// <summary> Totally finished issue </summary>
        Finished
    }
}