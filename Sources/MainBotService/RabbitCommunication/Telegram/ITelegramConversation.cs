using System.Threading.Tasks;

namespace MainBotService.RabbitCommunication.Telegram
{
    /// <summary> Telegram conversation </summary>
    public interface ITelegramConversation
    {
        /// <summary> Is the messageTest for this conversation? </summary>
        /// <param name="messageText">Text from telegram message</param>
        Task<bool> IsStartingMessage(string messageText);

        /// <summary> Restore conversation </summary>
        /// <param name="step">Step of conversation</param>
        /// <param name="outgoingPreMessageInfo">Information about message</param>
        /// <param name="messageText">Text from telegram message</param>
        /// <returns>Next step name. null - conversation is finished</returns>
        Task<string?> NextConversationStep(string step, 
            OutgoingPreMessageInfo outgoingPreMessageInfo,
            string messageText);
    }
}