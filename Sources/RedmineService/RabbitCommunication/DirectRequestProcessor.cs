using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMqInfrastructure;
using RedmineService.Redmine;
using Serilog;

namespace RedmineService.RabbitCommunication
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DirectRequestProcessor : IDirectRequestProcessor
    {
        private readonly INodeInfo _nodeInfo;
        private readonly ILogger _logger;
        private readonly RedmineCommunication _redmineService;

        public DirectRequestProcessor(INodeInfo nodeInfo, ILogger logger)
        {
            this._nodeInfo = nodeInfo;
            this._logger = logger;
            this._redmineService = new RedmineCommunication(logger);
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
                    return await this._redmineService.GetAnyInformation();
                }
                catch (Exception e)
                {
                    return "Error " + e.ToString();
                }
            }
            else if (actionName.ToUpper() == "ANY_QUERY")
            {
                try
                {
                    return (await this._redmineService.GetLastChangedIssues()).ToString();
                }
                catch (Exception e)
                {
                    return  "Error: " + e.Message + " " + e.ToString();
                    
                }
            }

            return "Response for " + directMessage;
        }
    }
}