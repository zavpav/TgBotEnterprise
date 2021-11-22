using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using JenkinsService.Jenkins;
using JenkinsService.RabbitCommunication;
using RabbitMqInfrastructure;
using Serilog;

namespace JenkinsService
{
    public class JenkinsWorker : BackgroundService
    {
        private readonly RabbitProcessor _rabbitProcessor;
        private readonly ILogger _logger;
        private readonly JenkinsCommunication _jenkinsCommunication;

        public JenkinsWorker(IRabbitProcessor rabbitProcessor, ILogger logger, JenkinsCommunication jenkinsCommunication)
        {
            this._rabbitProcessor = (RabbitProcessor)rabbitProcessor;
            this._logger = logger;
            this._jenkinsCommunication = jenkinsCommunication;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this._jenkinsCommunication.UpdateDb();

            while (!stoppingToken.IsCancellationRequested)
            {
                var jobsChanges = await this._jenkinsCommunication.UpdateDb();
                if (jobsChanges.Count != 0)
                    await this._rabbitProcessor.SendUpdatedJobs(jobsChanges);


                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
