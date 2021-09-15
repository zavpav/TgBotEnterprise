using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;

namespace RabbitMqInfrastructure
{
    public interface IRabbitService
    {
        /// <summary> Initialize </summary>
        void Initialize();

        /// <summary> Direct request for another service </summary>
        /// <param name="serviceType">Service type</param>
        /// <param name="actionName">Method name</param>
        /// <param name="message">Message</param>
        Task<string> DirectRequest(EnumInfrastructureServicesType serviceType, string actionName, string message);

        /// <summary> Publish information from node to CentralHub </summary>
        /// <param name="actionName">Method name</param>
        /// <param name="message">Information</param>
        Task PublishInformation(string actionName, string message);

        /// <summary> Subscribe to central information </summary>
        /// <param name="serviceType">Service type</param>
        /// <param name="actionName">Action. If null - subcribe all for service</param>
        /// <param name="processFunc">Func generate Task for processing message params[message, headers]</param>
        void Subscribe(EnumInfrastructureServicesType serviceType, string? actionName, Func<string, IDictionary<string, string>, Task> processFunc);
    }
}