using System.ComponentModel.DataAnnotations;

namespace MainBotService.Database
{
    public class DtoUserInfo
    {
        /// <summary> Synthetic ID </summary>
        [Required]
        public int Id { get; set; }

        /// <summary> User Id in MainBot </summary>
        public string? BotUserId { get; set; }

        /// <summary> Is User active? </summary>
        [Required]
        public bool IsActive { get; set; } = false;

        /// <summary> Describe </summary>
        public string? WhoIsThis { get; set; }

        /// <summary> Jenkins name </summary>
        public string? JenkinsUserName { get; set; }

        /// <summary> Redmine full name </summary>
        public string? RedmineUserName { get; set; }
    }
}