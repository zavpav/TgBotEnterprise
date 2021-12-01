using System;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RabbitMqInfrastructure;
using RedmineService.Database;
using RedmineService.Redmine;
using Serilog;

namespace RedmineService
{
    public class RedmineWorker : BackgroundService
    {
        private readonly RabbitProcessor _rabbitProcessor;
        private readonly IDbContextFactory<RedmineDbContext> _dbContextFactory;
        private readonly ILogger _logger;

        public RedmineWorker(IRabbitProcessor rabbitProcessor,
            IDbContextFactory<RedmineDbContext> dbContextFactory,
            ILogger logger)
        {
            this._rabbitProcessor = (RabbitProcessor)rabbitProcessor;
            this._dbContextFactory = dbContextFactory;
            this._logger = logger;
        }

        private int _timeOutProblemCount = 0;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var redmineCommunication = new RedmineCommunication(this._logger, this._dbContextFactory);

            await redmineCommunication.UpdateIssuesDb();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var changedIssues = await redmineCommunication.UpdateIssuesDb();
                    if (changedIssues.Count != 0)
                        await ((RabbitProcessor) this._rabbitProcessor).SendUpdatedIssues(changedIssues);

                    this._timeOutProblemCount = 0;
                }
                catch (Exception ex)
                {
                    await this._rabbitProcessor.SendProblemToAdmin(ex);
                    this._timeOutProblemCount = this._timeOutProblemCount * 5 + 5;
                    await Task.Delay(this._timeOutProblemCount * 1000, stoppingToken);
                }

                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10000, stoppingToken);
            }
        }

    }
}
