using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommonInfrastructure;

namespace JenkinsService.Database
{
    /// <summary> Project settings in Jenkins service </summary>
    public class DbeProjectSettings
    {
        // ReSharper disable once UnusedMember.Local
        private DbeProjectSettings() { }

        public DbeProjectSettings(string projectSysName)
        {
            this.ProjectSysName = projectSysName;
            this.JobInformations = new List<JobDescription>();
        }

        public DbeProjectSettings(string projectSysName, List<JobDescription> jobNames)
            : this(projectSysName)
        {
            this.JobInformations = jobNames.ToList();
        }


        /// <summary> Synthetic ID </summary>
        [Required]
        public int Id { get; set; }

        /// <summary> System name of project </summary>
        [Required]
        public string ProjectSysName { get; set; }

        /// <summary> System name of project </summary>
        public ICollection<JobDescription> JobInformations { get; set; }


        /// <summary> Information about job </summary>
        public class JobDescription
        {
            /// <summary> Synthetic ID </summary>
            [Required]
            public int Id { get; set; }

            /// <summary> Synthetic ParentId </summary>
            [Required]
            public DbeProjectSettings? Parent { get; set; }

            /// <summary> Type of jobs </summary>
            public EnumBuildServerJobs JobType { get; set; }
            
            /// <summary> Url part </summary>
            public string JobPath { get; set; }
        }
    }
}