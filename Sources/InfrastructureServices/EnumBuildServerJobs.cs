using System.ComponentModel;

namespace CommonInfrastructure
{
    /// <summary> BuildServer job types </summary>
    public enum EnumBuildServerJobs
    {
        /// <summary> Undef </summary>
        [Description("Undefind job")]
        Undef,

        /// <summary> Dump Job </summary>
        [Description("Job for extract dumps")]
        Dump,

        /// <summary> Build current version </summary>
        [Description("Job for Create current version")]
        Current,

        /// <summary> Build rc version </summary>
        [Description("Job for Rc version")]
        Rc,

        /// <summary> System job </summary>
        [Description("System job")]
        System
    }
}