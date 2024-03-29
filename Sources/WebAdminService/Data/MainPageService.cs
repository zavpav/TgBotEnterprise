﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication;
using RabbitMqInfrastructure;

namespace WebAdminService.Data
{
    public class MainPageService
    {
        private readonly IRabbitService _rabbitService;
        private readonly IGlobalEventIdGenerator _eventIdGenerator;

        public MainPageService(IRabbitService rabbitService, IGlobalEventIdGenerator eventIdGenerator)
        {
            this._rabbitService = rabbitService;
            this._eventIdGenerator = eventIdGenerator;
        }

        public async Task<ActualServicesInfo[]> GetActualServices()
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Tuple<EnumInfrastructureServicesType, Task<string>>>();

            var eventId = this._eventIdGenerator.GetNextEventId();
            foreach (var servicesType in Enum.GetValues<EnumInfrastructureServicesType>())
            {
                tasks.Add(Tuple.Create(servicesType, this._rabbitService.DirectRequest(servicesType, RabbitMessages.PingMessage, RabbitMessages.PingMessage, eventId)));
            }
            
            var result = new List<ActualServicesInfo>();
            foreach (var anyTsk in tasks)
            {
                var serviceType = anyTsk.Item1;
                try
                {
                    var res = await anyTsk.Item2;
                    result.Add(new ActualServicesInfo(serviceType, res));
                }
                catch (TimeoutException)
                {
                    result.Add(new ActualServicesInfo(serviceType, "Request failed Timout"));
                }
            }
            sw.Stop();
            
            return result.ToArray();
        }

        public struct ActualServicesInfo
        {
            public ActualServicesInfo(EnumInfrastructureServicesType serviceType, string pingResultStatusInfo)
            {
                this.ServiceType = serviceType;
                this.PingResultStatusInfo = pingResultStatusInfo;
            }

            public EnumInfrastructureServicesType ServiceType { get; }

            public string PingResultStatusInfo { get; }

        }



    }
}