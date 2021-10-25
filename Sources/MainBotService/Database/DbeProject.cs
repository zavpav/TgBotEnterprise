using System.ComponentModel.DataAnnotations;

namespace MainBotService.Database
{
    /// <summary> Describe project </summary>
    public class DbeProject
    {
        public DbeProject(string sysName, string description)
        {
            this.SysName = sysName;
            this.Description = description;
        }

        /// <summary> Synthetic ID </summary>
        [Required]
        public int Id { get; set; }

        /// <summary> System name of project </summary>
        [Required]
        public string SysName { get; set; }

        /// <summary> Name for people </summary>
        [Required]
        public string Description { get; set; }

        /// <summary> Current version Number (as in Redmine) </summary>
        public string? CurrentVersion { get; set; }
        
        /// <summary> Rc version Number (as in Redmine) </summary>
        public string? RcVersion { get; set; }
    }
}