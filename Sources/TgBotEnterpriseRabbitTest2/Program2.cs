using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfrastructureServices;
using RabbitMqInfrastructure;

namespace TgBotEnterpriseRabbitTest2
{
    class Program2
    {
        static void Main(string[] args)
        {
            var nodeInfo = new NodeInfo("T2", EnumInfrastructureServicesType.BuildService, null);
            Console.WriteLine($"{nodeInfo.ServicesType} starting...");

            var rabbitService = (IRabbitService)new RabbitService(nodeInfo, "localhost", new DirectRequestProcessor2());
            rabbitService.Initialize();
            Console.WriteLine($"{nodeInfo.ServicesType} end Main...");
            Console.ReadLine();
        }

        private class DirectRequestProcessor2 : IDirectRequestProcessor
        {
            ///// <summary> Generate function for generate task for processing message </summary>
            ///// <param name="actionName"></param>
            ///// <returns></returns>
            ///// <remarks>
            ///// return func for generate function
            /////
            ///// await func(rabbitService, rawMessageBody, parsedJson, typedObject)
            ///// </remarks>
            //public Func<IRabbitService, string, Task<string>> ProcessingFactoryGenerator(string actionName)
            //{

            //}

            public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit, string actionName,
                IDictionary<string, string> messageHeaders, string directMessage)
            {
                
                Console.WriteLine("Method " + actionName);
                Console.WriteLine("Megaprocessing " + directMessage);
                
                await Task.Delay(5000);

                return directMessage + ".........";
            }
        }
    }
}
