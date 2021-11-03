using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;

namespace RabbitMqInfrastructure
{
    /// <summary> Delegate for processing rabbit messages </summary>
    /// <param name="message">Message body</param>
    /// <param name="rabbitMessageHeaders">Rabbit headers</param>
    public delegate Task ProcessMessage(string message, IDictionary<string, string> rabbitMessageHeaders);


    /// <summary> Delegate for processing sync rabbit messages </summary>
    /// <param name="message">Message body</param>
    /// <param name="rabbitMessageHeaders">Rabbit headers</param>
    /// <returns>Output message body</returns>
    public delegate Task<string> DirectProcessMessage(string message, IDictionary<string, string> rabbitMessageHeaders);

    public interface IRabbitService
    {
        /// <summary> Initialize </summary>
        void Initialize();

        /// <summary> Direct request for another service </summary>
        /// <param name="serviceType">Service type</param>
        /// <param name="actionName">Method name</param>
        /// <param name="message">Message</param>
        /// <param name="eventId">Unique event id</param>
        Task<string> DirectRequest(EnumInfrastructureServicesType serviceType, string actionName, string message, string? eventId = null);

        /// <summary> Publish information from node to CentralHub </summary>
        /// <param name="actionName">Method name</param>
        /// <param name="message">Information</param>
        /// <param name="subscriberServiceType">Subscriber type</param>
        /// <param name="eventId">Unique event id</param>
        Task PublishInformation(string actionName, string message, EnumInfrastructureServicesType? subscriberServiceType, string? eventId = null);

        /// <summary> Subscribe to central information </summary>
        /// <param name="publisherServiceType">Service type</param>
        /// <param name="actionName">Action. If null - subcribe all for service</param>
        /// <param name="processFunc">Func generate Task for processing message params[message, headers]</param>
        void Subscribe(EnumInfrastructureServicesType? publisherServiceType, string? actionName, ProcessMessage processFunc);

        /// <summary> Register direct message processor </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="processFunc">Func generate Task for processing message params[message, headers]</param>
        void RegisterDirectProcessor(string actionName, DirectProcessMessage processFunc);
    }
}