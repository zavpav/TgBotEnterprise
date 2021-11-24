using System;
using System.Net;
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

        private int _timeOutProblemCount = 0;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this._jenkinsCommunication.UpdateDb();

            //await this._jenkinsCommunication.UpdateGitCommentInfo();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var jobsChanges = await this._jenkinsCommunication.UpdateDb();
                    if (jobsChanges.Count != 0)
                        await this._rabbitProcessor.SendUpdatedJobs(jobsChanges);
                    this._timeOutProblemCount = 0;
                }
                catch (Exception ex)
                {
                    await this._rabbitProcessor.SendProblemToAdmin(ex);
                    this._timeOutProblemCount++;
                    await Task.Delay(this._timeOutProblemCount * 5000, stoppingToken);
                }


                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
