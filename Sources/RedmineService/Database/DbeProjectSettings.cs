﻿using System.ComponentModel.DataAnnotations;

namespace RedmineService.Database
{
    /// <summary> Project settings in Redmine service </summary>
    public class DbeProjectSettings
    {
        // ReSharper disable once UnusedMember.Local
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private DbeProjectSettings() { }
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public DbeProjectSettings(string projectSysName)
        {
            this.ProjectSysName = projectSysName;
            this.RedmineProjectName = "";
            this.VersionMask = "";
        }

        /// <summary> Synthetic ID </summary>
        [Required]
        public int Id { get; set; }

        /// <summary> System name of project </summary>
        [Required]
        public string ProjectSysName { get; set; }

        /// <summary> Project name in redmine service </summary>
        [Required]
        public string RedmineProjectName { get; set; }

        /// <summary> Version mask </summary>
        /// <returns>In my team's redmine I had two types of projects in single name. We separated them by version name</returns>
        public string VersionMask { get; set; }

        /// <summary> Redmine projectId </summary>
        public int? RedmineProjectId { get; set; }

    }
}