using System.Threading.Tasks;

namespace MainBotService.RabbitCommunication.TelegramDialoges
{
    /// <summary> Telegram conversation </summary>
    public interface ITelegramConversation
    {
        /// <summary> Is the messageTest for this conversation? </summary>
        /// <param name="messageText">Text from telegram message</param>
        Task<bool> IsStartingMessage(string messageText);

        /// <summary> Restore conversation </summary>
        /// <param name="step">Step of conversation</param>
        /// <param name="messageText">Text from telegram message</param>
        Task<string?> NextConversationStep(string step, string messageText);
    }
}