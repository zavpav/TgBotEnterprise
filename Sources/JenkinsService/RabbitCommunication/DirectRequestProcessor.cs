using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMqInfrastructure;

namespace JenkinsService.RabbitCommunication
{
    public class DirectRequestProcessor : IDirectRequestProcessor
    {
        private readonly INodeInfo _nodeInfo;

        public DirectRequestProcessor(INodeInfo nodeInfo)
        {
            this._nodeInfo = nodeInfo;
        }

        public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit,
            string actionName,
            IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            await Task.Delay(9000);
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");
            return directMessage;
        }
    }
}