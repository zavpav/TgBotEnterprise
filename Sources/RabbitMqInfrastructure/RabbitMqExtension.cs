using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication;
using Serilog;

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
        /// <param name="logger">Logger</param>
        public static void Subscribe<T>(this IRabbitService rabbitService, 
            EnumInfrastructureServicesType? serviceType, 
            string actionName,
            ProcessMessage<T> messageProcessor,
            ILogger logger)
            where T : IRabbitMessage
        {
            rabbitService.Subscribe(serviceType, actionName, 
                async (message, rabbitHeaders) =>
                {
                    try
                    {
                        var msgData = JsonSerializer2.DeserializeRequired<T>(message, logger);
                        await messageProcessor(msgData, rabbitHeaders);
                    }
                    catch (Exception e)
                    {
                        string? eventId = null;
                        try
                        {
                            var emptyMessage = JsonSerializer2.DeserializeRequired<EmptyMessage>(message, logger);
                            eventId = emptyMessage.SystemEventId;
                        }
                        catch 
                        {
                            // ignore
                        }

                        logger
                            .ForContext("message", message)
                            .ErrorWithEventContext(eventId, e, "Error while processing message {actionName}", actionName);
                        throw;
                    }
                });
        }

        public delegate Task<TOut> ProcessDirectMessage<TIn, TOut>(TIn message, IDictionary<string, string> rabbitMessageHeaders);

        /// <summary> Subscribe to rabbit events </summary>
        /// <typeparam name="TIn">Income message type</typeparam>
        /// <typeparam name="TOut">Outgoing message type</typeparam>
        /// <param name="rabbitService">Rabbit service</param>
        /// <param name="actionName">Action</param>
        /// <param name="messageProcessor">Message processor</param>
        /// <param name="logger">Logger</param>
        public static void RegisterDirectProcessor<TIn, TOut>(this IRabbitService rabbitService, 
            string actionName, 
            ProcessDirectMessage<TIn, TOut> messageProcessor,
            ILogger logger)
        where TIn: IRabbitMessage
        where TOut: IRabbitMessage
        {
            rabbitService.RegisterDirectProcessor(actionName, async (message, rabbitHeaders) =>
            {
                try
                {
                    var msgData = JsonSerializer2.DeserializeRequired<TIn>(message, logger);
                    var result = await messageProcessor(msgData, rabbitHeaders);
                    return JsonSerializer.Serialize(result);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error while processing message {actionName} Processing message {@message}", actionName, message);
                    throw;
                }
            });
        }

        public static async Task PublishInformation<T>(this IRabbitService rabbitService, string actionName, T messageData, EnumInfrastructureServicesType? subscriberServiceType = null)
        where T : IRabbitMessage
        {
            var jsonMessage = JsonSerializer.Serialize(messageData);
            await rabbitService.PublishInformation(actionName, jsonMessage, subscriberServiceType, messageData.SystemEventId);
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
            return await rabbitService.DirectRequestTo<TI, TO>(EnumInfrastructureServicesType.Main, actionName, messageData);
        }

        /// <summary> Direct request for another service </summary>
        /// <typeparam name="TI">Request message type</typeparam>
        /// <typeparam name="TO">Response message type</typeparam>
        /// <param name="rabbitService">Rabbit service</param>
        /// <param name="serviceType">Service type</param>
        /// <param name="actionName">Method name</param>
        /// <param name="message">Message</param>
        public static async Task<TO> DirectRequestTo<TI, TO>(this IRabbitService rabbitService, 
                EnumInfrastructureServicesType serviceType,
                string actionName, 
                TI message)
            where TI : IRabbitMessage
            where TO : IRabbitMessage
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            var responseStr = await rabbitService.DirectRequest(serviceType, actionName, jsonMessage, message.SystemEventId);
            var response = JsonSerializer.Deserialize<TO>(responseStr);
            return response ?? throw new NotSupportedException("Empty response or deserialization problem");
        }

        ///// <summary> Direct request for another service </summary>
        ///// <typeparam name="TO">Response message type</typeparam>
        ///// <param name="rabbitService">Rabbit service</param>
        ///// <param name="serviceType">Service type</param>
        ///// <param name="actionName">Method name</param>
        ///// <param name="message">Message</param>
        //public static async Task<TO> DirectRequestTo<TO>(this IRabbitService rabbitService,
        //    EnumInfrastructureServicesType serviceType,
        //    string actionName,
        //    IRabbitMessage message)
        //    where TO : IRabbitMessage
        //{
        //    var jsonMessage = JsonSerializer.Serialize(message);
        //    var responseStr = await rabbitService.DirectRequest(serviceType, actionName, jsonMessage, message.SystemEventId);
        //    var response = JsonSerializer.Deserialize<TO>(responseStr);
        //    return response ?? throw new NotSupportedException("Empty response or deserialization problem");
        //}

    }
}