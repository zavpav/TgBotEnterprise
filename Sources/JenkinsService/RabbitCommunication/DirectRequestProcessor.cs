using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using JenkinsService.Jenkins;
using RabbitMqInfrastructure;
using Serilog;

namespace JenkinsService.RabbitCommunication
{
    public class DirectRequestProcessor : IDirectRequestProcessor
    {
        private readonly INodeInfo _nodeInfo;
        private readonly ILogger _logger;
        private readonly JenkinsCommunication _jenkinsCommunication;

        public DirectRequestProcessor(INodeInfo nodeInfo, ILogger logger)
        {
            this._logger = logger;
            this._nodeInfo = nodeInfo;
            this._jenkinsCommunication = new JenkinsCommunication(logger);
        }

        public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit,
            string actionName,
            IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");

            if (actionName.ToUpper() == "PING")
            {
                try
                {
                    return await this._jenkinsCommunication.GetAnyInformation();

                }
                catch (Exception e)
                {
                    return "Error " + e.ToString();
                }
            }
            else
            {
                await Task.Delay(9000);
                Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");
                return directMessage;
            }
        }
    }
}