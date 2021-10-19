using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMqInfrastructure
{
    /// <summary> Rabbot message processor  </summary>
    public interface IRabbitProcessor
    {
        /// <summary> Process direct messages </summary>
        /// <param name="rabbit">Reference to rabbit service</param>
        /// <param name="actionName">Action name</param>
        /// <param name="messageHeaders">Message headers</param>
        /// <param name="directMessage">Raw Message text</param>
        /// <returns></returns>
        Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit, 
            string actionName,
            IDictionary<string, string> messageHeaders, 
            string directMessage);

        /// <summary> Subscribe on other services messages  </summary>
        void Subscribe();

    }
}