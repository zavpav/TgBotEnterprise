using System;
using System.Collections.Generic;
using CommonInfrastructure;

namespace JenkinsService.Database
{
    /// <summary> Information about executed(-ing) jobs </summary>
    public class DbeJenkinsJob
    {
        /// <summary> Synthetic id </summary>
        public int Id { get; set; }


        /// <summary> Jenkins name of project </summary>
        public string JenkinsProjectName { get; set; } = "";
        
        /// <summary> Information about user (or scm) who started job </summary>
        public string JenkinsBuildStarter { get; set; } = "";


        /// <summary> Number of job excution </summary>
        public string BuildNumber { get; set; } = "";

        /// <summary> Build name </summary>
        /// <remarks> Almost it is "#{jobNum}", but sometimes it may contains some information liked "#{jobNum} {database}" (for dumps) </remarks>
        public string BuildName { get; set; } = "";

        /// <summary> Build description </summary>
        /// <remarks>Almost it is empty, but sometimes it may contains some information about dump file etc (for dumps) </remarks>
        public string BuildDescription { get; set; } = "";

        /// <summary> Status of execution (SUCCESS, FAILURE, ABORTED) </summary>
        public string BuildStatus { get; set; } = "";

        /// <summary> Information that the build is processing now </summary>
        public bool BuildIsProcessing { get; set; }

        /// <summary> Duration of executing </summary>
        public TimeSpan BuildDuration { get; set; }

        /// <summary> Git branch name </summary>
        public string BuildBranchName { get; set; } = "";

        /// <summary> Project sysname from bot system </summary>
        public string? ProjectSysName { get; set; }

        /// <summary> Type of build </summary>
        public EnumBuildServerJobs BuildTupe { get; set; }

        /// <summary> BotSystemUserId Whom assign issue  </summary>
        public string? UserBotIdAssignOn { get; set; }

        /// <summary> Information about changes are included into build </summary>
        public ICollection<ChangeInfo>? ChangeInfos { get; set; }


        /// <summary> Information about changes from SCM </summary>
        /// <remarks>
        /// According to our requirements, the comment must have the format:
        /// {projectName} #{issueId} {Other information}
        /// Properties ProjectName and issueId is filled from comment and may be empty (when teammember doesn't write comment as needed)
        /// </remarks>
        public class ChangeInfo
        {
            /// <summary> Synthetic id </summary>
            public int Id { get; set; }

            /// <summary> Reference to owner </summary>
            public int JenkinsJobId { get; set; }
            
            /// <summary> Owner </summary>
            public DbeJenkinsJob? JenkinsJob { get; set; }

            
            /// <summary> Comment from git loaded by jenkins </summary>
            public string GitComment { get; set; } = "";

            /// <summary> Project name from comment </summary>
            public string? ProjectName { get; set; }

            /// <summary> Issue id from comment </summary>
            public string? IssueId { get; set; }
        }
    }
}