using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMqInfrastructure
{
    public interface IDirectRequestProcessor
    {
        /// <summary>
        /// Process messages
        /// </summary>
        /// <param name="rabbit">Reference to rabbit service</param>
        /// <param name="actionName">Action name</param>
        /// <param name="messageHeaders">Message headers</param>
        /// <param name="directMessage">Raw Message text</param>
        /// <returns></returns>
        Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit, 
            string actionName,
            IDictionary<string, string> messageHeaders, 
            string directMessage);
    }

    public class DirectRequestProcessorStub : IDirectRequestProcessor
    {
        public Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit, string actionName, IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            Console.WriteLine(actionName);
            return Task.FromResult("directMessage");
        }
    }
}