using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;

namespace MainBotService.RabbitCommunication.Telegram
{
    /// <summary> First simple conversation </summary>
    public class MyTasksConversation : ITelegramConversation
    {
        private readonly IMainBotService _mainBot;
        private readonly ILogger _logger;

        private Regex ReFirstMessage { get; }

        public MyTasksConversation(IMainBotService mainBot, ILogger logger)
        {
            this._mainBot = mainBot;
            this._logger = logger;
            this.ReFirstMessage = new Regex("^мои задачи|my tasks$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public Task<bool> IsStartingMessage(string messageText)
        {
            var isConversation = this.ReFirstMessage.IsMatch(messageText);
            return Task.FromResult(isConversation);
        }

        public Task<string?> NextConversationStep(string step, OutgoingPreMessageInfo outgoingPreMessageInfo,
            string messageText)
        {
            this._logger.Information("MyTasksConversation");
            return Task.FromResult((string?)null);
        }
    }
}