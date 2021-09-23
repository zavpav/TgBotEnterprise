using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TelegramService.Telegram;

namespace TelegramService
{
    public class TgPullWorker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ITelegramWrap _telegramWrap;

        public TgPullWorker(ILogger logger, ITelegramWrap telegramWrap)
        {
            this._logger = logger;
            this._telegramWrap = telegramWrap;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await this._telegramWrap.Pull();
//                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
