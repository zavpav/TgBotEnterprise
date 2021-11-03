using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using RabbitMqInfrastructure;
using Serilog;

namespace JenkinsService
{
    public class JenkinsWorker : BackgroundService
    {
        private readonly IRabbitProcessor _rabbitProcessor;
        private readonly ILogger _logger;

        public JenkinsWorker(IRabbitProcessor rabbitProcessor, ILogger logger)
        {
            this._rabbitProcessor = rabbitProcessor;
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
