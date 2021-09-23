using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Requests;

namespace TelegramService.Database
{
    public class DtoUserInfo
    {
        /// <summary> Syntetic ID </summary>
        [Required]
        public int Id { get; set; }
        
        /// <summary> Telegram User Id </summary>
        [Required]
        public long TelegramUserId { get; set; }

        /// <summary> Is User active? </summary>
        [Required]
        public bool IsActive { get; set; } = false;

        /// <summary> Default Chat Id for user. If it is empty It's means chat undefined and won't send any information from Redmine or Jenkins services </summary>
        public long? DefaultChatId  {get; set;}

        /// <summary> Self-presentation </summary>
        public string? WhoIsThis { get; set; }

        /// <summary> User Id in MainBot </summary>
        public string? BotUserId { get; set; }
    }
}