namespace InfrastructureServices
{
    public interface INodeInfo
    {
        /// <summary> Main type of sevice </summary>
        EnumInfrastructureServicesType ServicesType { get; }

        /// <summary> Service subtype. E.g. for BuildService it may be Jenkins and TeamCity </summary>
        string ServiceSubType { get; }

        /// <summary> Unique name for node </summary>
        string NodeName { get; }
    }

    public class NodeInfo : INodeInfo
    {
        public NodeInfo(string nodeName, EnumInfrastructureServicesType servicesType, string serviceSubType)
        {
            this.NodeName = nodeName;
            this.ServicesType = servicesType;
            this.ServiceSubType = serviceSubType;
        }

        public string NodeName { get; }
        public EnumInfrastructureServicesType ServicesType { get; }
        public string? ServiceSubType { get; }
    }
}