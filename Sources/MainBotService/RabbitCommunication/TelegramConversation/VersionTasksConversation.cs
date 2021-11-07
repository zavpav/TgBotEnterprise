using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Serilog;

namespace MainBotService.RabbitCommunication.TelegramDialoges
{
    public class VersionTasksConversation : ITelegramConversation
    {
        private readonly IMainBotService _mainBot;
        private readonly ILogger _logger;
        private Regex ReFirstMessage { get; }

        public VersionTasksConversation(IMainBotService mainBot, ILogger logger)
        {
            this._mainBot = mainBot;
            this._logger = logger;
            this.ReFirstMessage = new Regex("^версия|version$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public Task<bool> IsStartingMessage(string messageText)
        {
            var isConversation = this.ReFirstMessage.IsMatch(messageText);
            return Task.FromResult(isConversation);
        }

        public Task<string?> NextConversationStep(string step, string messageText)
        {
            this._logger.Information("VersionTasksConversation");
            return Task.FromResult((string?)null);
        }
    }
}