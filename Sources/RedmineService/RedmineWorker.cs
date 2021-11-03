using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using RabbitMqInfrastructure;
using Serilog;

namespace RedmineService
{
    public class RedmineWorker : BackgroundService
    {
        private readonly IRabbitProcessor _rabbitProcessor;
        private readonly ILogger _logger;

        public RedmineWorker(IRabbitProcessor rabbitProcessor, ILogger logger)
        {
            this._rabbitProcessor = rabbitProcessor;
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

    }
}
