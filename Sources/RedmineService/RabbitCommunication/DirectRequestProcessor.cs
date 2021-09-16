using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMqInfrastructure;
using RedmineService.Redmine;

namespace RedmineService.RabbitCommunication
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DirectRequestProcessor : IDirectRequestProcessor
    {
        private readonly INodeInfo _nodeInfo;
        private RedmineCommunication _redmineService;

        public DirectRequestProcessor(INodeInfo nodeInfo)
        {
            this._nodeInfo = nodeInfo;
            this._redmineService = new RedmineCommunication();
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