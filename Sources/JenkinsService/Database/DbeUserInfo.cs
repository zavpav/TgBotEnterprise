using System;
using System.ComponentModel.DataAnnotations;

namespace JenkinsService.Database
{
    public class DbeUserInfo
    {
        /// <summary> Synthetic ID </summary>
        [Required]
        public int Id { get; set; }

        /// <summary> User Id in MainBot </summary>
        public string BotUserId { get; set; }

        /// <summary> Is User active? </summary>
        [Required]
        public bool IsActive { get; set; } = false;

        /// <summary> Self-presentation </summary>
        public string? WhoIsThis { get; set; }



        /// <summary> Jenkins name </summary>
        public string? JenkinsName { get; set; }
    }
}