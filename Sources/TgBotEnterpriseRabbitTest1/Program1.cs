using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMqInfrastructure;

namespace TgBotEnterprise
{
    class Program1
    {
        static void Main(string[] args)
        {
            var nodeInfo = new NodeInfo("T1", EnumInfrastructureServicesType.Main);

            Console.WriteLine($"{nodeInfo.ServicesType} starting...");

            var rabbitService = (IRabbitService) new RabbitService(nodeInfo, "localhost", new DirectRequestProcessor1());
            rabbitService.Initialize();


            var respTask = rabbitService.DirectRequest(EnumInfrastructureServicesType.BuildService,
                "Processss",
                "Привепт.123ываasdfdas"
            );

            var res = respTask.GetAwaiter().GetResult();
            Console.WriteLine(res);


            respTask = rabbitService.DirectRequest(EnumInfrastructureServicesType.BugTracker,
                "Processss",
                "Привепт.123ываasdfdas"
            );


            res = respTask.GetAwaiter().GetResult();
            Console.WriteLine(res);


            Console.WriteLine($"{nodeInfo.ServicesType} end Main...");
            Console.ReadLine();
        }
    }

    internal class DirectRequestProcessor1 : IDirectRequestProcessor
    {
        public Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit, string actionName,
            IDictionary<string, string> messageHeaders, string directMessage)
        {
            throw new NotImplementedException();
        }
    }
}
