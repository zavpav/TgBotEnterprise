using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication;

namespace RabbitMqInfrastructure
{
    public static class RabbitMqExtension
    {
        public delegate Task ProcessMessage<T>(T message, IDictionary<string, string> rabbitMessageHeaders);

        /// <summary> Subscribe to rabbit events </summary>
        /// <typeparam name="T">MessageType</typeparam>
        /// <param name="rabbitService">Rabbit service</param>
        /// <param name="serviceType">Publisher service type</param>
        /// <param name="actionName">Action</param>
        /// <param name="messageProcessor">Message processor</param>
        public static void Subscribe<T>(this IRabbitService rabbitService, 
            EnumInfrastructureServicesType serviceType, 
            string actionName,
            ProcessMessage<T> messageProcessor)
        {
            rabbitService.Subscribe(serviceType, actionName, 
                (message, rabbitHeaders) =>
                {
                    var msgData = JsonSerializer.Deserialize<T>(message) ?? throw new NotSupportedException("No data for deserialization");
                    return messageProcessor(msgData, rabbitHeaders);
                });
        }

        public static async Task PublishInformation<T>(this IRabbitService rabbitService, string actionName, T messageData)
        where T : IRabbitMessage
        {
            var jsonMessage = JsonSerializer.Serialize(messageData);
            await rabbitService.PublishInformation(actionName, jsonMessage, messageData.SystemEventId);
        }

        /// <summary> Direct request to MainBot </summary>
        /// <typeparam name="TI">Request message type</typeparam>
        /// <typeparam name="TO">Response message type</typeparam>
        /// <param name="rabbitService">Rabbit service</param>
        /// <param name="actionName">Action</param>
        /// <param name="messageData">Request message</param>
        /// <returns>Response message</returns>
        public static async Task<TO> DirectRequestToMainBot<TI, TO>(this IRabbitService rabbitService,
            string actionName, TI messageData)
        where TI : IRabbitMessage
        where TO : IRabbitMessage
        {
            var jsonMessage = JsonSerializer.Serialize(messageData);
            var responseStr = await rabbitService.DirectRequest(EnumInfrastructureServicesType.Main, actionName, jsonMessage, messageData.SystemEventId);
            var response = JsonSerializer.Deserialize<TO>(responseStr);
            return response ?? throw new NotSupportedException("Empty response or deserialization problem");
        }

    }
}