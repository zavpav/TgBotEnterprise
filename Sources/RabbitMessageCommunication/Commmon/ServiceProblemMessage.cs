using System;
using CommonInfrastructure;

namespace RabbitMessageCommunication.Commmon
{
    /// <summary> Information about some problem in any service. The service send it to MainBot. </summary>
    public class ServiceProblemMessage : IRabbitMessage
    {
        public ServiceProblemMessage(string systemEventId, EnumInfrastructureServicesType servicesType, string nodeName)
        {
            this.SystemEventId = systemEventId;
            this.ServicesType = servicesType;
            this.NodeName = nodeName;
        }

        /// <summary> Event Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Main type of sevice </summary>
        public EnumInfrastructureServicesType ServicesType { get;  }

        /// <summary> Unique name for node </summary>
        public string NodeName { get; }
        
        /// <summary> Some handwriting information </summary>
        public string? Description { get; set; }

        /// <summary> Exception type </summary>
        public string? ExceptionTypeName { get; set; }

        /// <summary> Some informaiton </summary>
        public string? ExceptionString { get; set; }

        /// <summary> StackTrace if exists </summary>
        public string? ExceptionStackTrace { get; set; }
    }
}