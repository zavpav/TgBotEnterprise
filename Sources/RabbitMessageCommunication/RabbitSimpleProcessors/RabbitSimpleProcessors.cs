using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMessageCommunication.RabbitSimpleProcessors
{
    /// <summary>
    /// Some common rabbit message processors
    /// </summary>
    public static class RabbitSimpleProcessors
    {
        /// <summary> Direct ping message processor. Just return "OK" </summary>
        public static Task<string> DirectPingProcessor(string message, IDictionary<string, string> rabbitMessageHeaders)
        {
            return Task.FromResult("OK");
        }
    }
}