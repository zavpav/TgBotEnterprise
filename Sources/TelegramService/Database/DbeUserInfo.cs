using System.ComponentModel.DataAnnotations;

namespace TelegramService.Database
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
        

        /// <summary> Telegram User Id </summary>
        [Required]
        public long TelegramUserId { get; set; }

        /// <summary> Default Chat Id for user. If it is empty It's means chat undefined and won't send any information from Redmine or Jenkins services </summary>
        public long? DefaultChatId  {get; set;}
    }
}