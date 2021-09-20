using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication;
using RabbitMqInfrastructure;

namespace MainBotService.Bot
{
    public static class RabbitMainBotConfigurationExtension
    {
        public static void RabbitSubscribe(this IMainBot mainBot, IRabbitService rabbitService)
        {
            rabbitService.Subscribe(EnumInfrastructureServicesType.Messaging, 
                RabbitMessages.TelegramMessageReceived,
                new ProcessMessage((msg, rabbitMsgHeaders) =>
                    {
                        return Task.CompletedTask;
                    })
                );
        }
    }
}