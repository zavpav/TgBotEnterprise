using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using RabbitMqInfrastructure;
using RedmineService.Database;
using RedmineService.Redmine;
using Serilog;

namespace RedmineService
{
    public class RedmineWorker : BackgroundService
    {
        private readonly IRabbitProcessor _rabbitProcessor;
        private readonly RedmineDbContext _dbContext;
        private readonly ILogger _logger;

        public RedmineWorker(IRabbitProcessor rabbitProcessor, 
            RedmineDbContext dbContext,
            ILogger logger)
        {
            this._rabbitProcessor = rabbitProcessor;
            this._dbContext = dbContext;
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var redmineCommunication = new RedmineCommunication(this._logger, this._dbContext);

            await redmineCommunication.UpdateIssuesDb();

            while (!stoppingToken.IsCancellationRequested)
            {
                var changedIssues = await redmineCommunication.UpdateIssuesDb();
                if (changedIssues.Count != 0)
                    await ((RabbitProcessor) this._rabbitProcessor).SendUpdatedIssues(changedIssues);
                
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10000, stoppingToken);
            }
        }

    }
}
