using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramService.Telegram;

namespace TelegramService
{
    public class TgPullWorker : BackgroundService
    {
        private readonly ILogger<TgPullWorker> _logger;
        private readonly ITelegramWrap _telegramWrap;

        public TgPullWorker(ILogger<TgPullWorker> logger, ITelegramWrap telegramWrap)
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
