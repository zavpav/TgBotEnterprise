namespace RabbitMessageCommunication.BuildService
{
    /// <summary> Statuses of builds in bot system </summary>
    public enum EnumBuildStatus
    {
        /// <summary> Undefined status or not defined</summary>
        NotDefined,
        
        /// <summary> Success build </summary>
        Success,
        
        /// <summary> Ok build with some heavy warnings </summary>
        Warning,
        
        /// <summary> Fail build </summary>
        Failure,
        
        /// <summary> Build is progressing now </summary>
        Processing,
        
        /// <summary> Build is aborted </summary>
        Aborted
    }
}