﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using RabbitMessageCommunication;
using RabbitMessageCommunication.RabbitSimpleProcessors;
using RabbitMqInfrastructure;
using Serilog;

namespace WebAdminService
{ 
    public class RabbitProcessor : IRabbitProcessor
    {
        private readonly ILogger _logger;
        private readonly INodeInfo _nodeInfo;
        private readonly IMapper _mapper;
        private readonly IRabbitService _rabbitService;

        public RabbitProcessor(ILogger logger,
            INodeInfo nodeInfo,
            IRabbitService rabbitService,
            IMapper mapper)
        {
            this._logger = logger;
            this._nodeInfo = nodeInfo;
            this._rabbitService = rabbitService;
            this._mapper = mapper;
        }


        public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit,
            string actionName,
            IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");

            await Task.Delay(9000);
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");
            return directMessage;
        }

        public void Subscribe()
        {
            this._rabbitService.RegisterDirectProcessor(RabbitMessages.PingMessage, RabbitSimpleProcessors.DirectPingProcessor);
        }
    }
}