using System.ComponentModel.DataAnnotations;

namespace RedmineService.Database
{
    public class DtoUserInfo
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

        /// <summary> Redmine name </summary>
        public string? RedmineName { get; set; }
    }
}