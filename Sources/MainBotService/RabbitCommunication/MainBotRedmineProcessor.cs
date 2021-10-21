using Serilog;

namespace MainBotService.RabbitCommunication
{
    public partial class MainBotService
    {
        public class RedmineProcessor
        {
            private readonly MainBotService _owner;
            private readonly ILogger _logger;

            public RedmineProcessor(MainBotService owner, ILogger logger)
            {
                this._owner = owner;
                this._logger = logger;
            }

        }


    }
}