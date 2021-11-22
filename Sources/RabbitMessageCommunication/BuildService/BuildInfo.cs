using System;
using CommonInfrastructure;

namespace RabbitMessageCommunication.BuildService
{
    /// <summary> Information about changed build from Build System </summary>
    public class BuildInfo
    {
        /// <summary> Project sysname from bot system </summary>
        public string? ProjectSysName { get; set; }

        /// <summary> Type of build </summary>
        public EnumBuildServerJobs BuildSubType { get; set; }

        /// <summary> Status of build </summary>
        public EnumBuildStatus BuildStatus { get; set; }

        /// <summary> Information about user (or scm) who started job </summary>
        public string ExecuterInfo { get; set; } = "";

        /// <summary> Information that the build is processing now </summary>
        public bool IsProgressing { get; set; }

        /// <summary> Job name of project (may be several for a botproject) </summary>
        public string JobName { get; set; } = "";

        /// <summary> Number of job excution </summary>
        public string BuildNumber { get; set; } = "";

        /// <summary> Build name </summary>
        /// <remarks> Almost it is "#{jobNum}", but sometimes it may contains some information liked "#{jobNum} {database}" (for dumps) </remarks>
        public string BuildName { get; set; } = "";

        /// <summary> Build description </summary>
        /// <remarks>Almost it is empty, but sometimes it may contains some information about dump file etc (for dumps) </remarks>
        public string BuildDescription { get; set; } = "";

        /// <summary> Duration of executing </summary>
        public TimeSpan BuildDuration { get; set; }

        /// <summary> Information about changes are included into build </summary>
        public ChangeInfo[]? ChangeInfos { get; set; }

        /// <summary> Information about changes from SCM </summary>
        /// <remarks>
        /// According to our requirements, the comment must have the format:
        /// {projectName} #{issueId} {Other information}
        /// Properties ProjectName and issueId is filled from comment and may be empty (when teammember doesn't write comment as needed)
        /// </remarks>
        public class ChangeInfo
        {
            /// <summary> Comment from git loaded by jenkins </summary>
            public string GitComment { get; set; } = "";

            /// <summary> Project name from comment </summary>
            public string? ProjectName { get; set; }

            /// <summary> Issue id from comment </summary>
            public string? IssueId { get; set; }
        }

    }
}