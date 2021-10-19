using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RabbitMqInfrastructure;
using Serilog;

namespace MainBotService
{
    public class MainBotWorker : BackgroundService
    {
        private readonly IRabbitProcessor _rabbitProcessor;
        private readonly ILogger _logger;

        public MainBotWorker(IRabbitProcessor rabbitProcessor, ILogger logger)
        {
            this._rabbitProcessor = rabbitProcessor;
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._rabbitProcessor.Subscribe();

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("MainBotService running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

    }
}