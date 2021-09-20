using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommonInfrastructure;

namespace RabbitMqInfrastructure
{
    public static class RabbitMqExtension
    {
        public delegate Task ProcessMessage<T>(T message, IDictionary<string, string> rabbitMessageHeaders);

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
        {
            var jsonMessage = JsonSerializer.Serialize(messageData);
            await rabbitService.PublishInformation(actionName, jsonMessage);
        }

    }
}