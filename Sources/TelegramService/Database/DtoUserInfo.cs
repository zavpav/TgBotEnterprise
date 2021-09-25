using System;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Requests;

namespace TelegramService.Database
{
    public class DtoUserInfo
    {
        /// <summary> Synthetic ID </summary>
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

        protected bool Equals(DtoUserInfo other)
        {
            return TelegramUserId == other.TelegramUserId 
                   && IsActive == other.IsActive 
                   && DefaultChatId == other.DefaultChatId 
                   && WhoIsThis == other.WhoIsThis 
                   && BotUserId == other.BotUserId;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DtoUserInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TelegramUserId, IsActive, DefaultChatId, WhoIsThis, BotUserId);
        }
    }
}