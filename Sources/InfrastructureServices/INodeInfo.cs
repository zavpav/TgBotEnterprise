namespace CommonInfrastructure
{
    public interface INodeInfo
    {
        /// <summary> Main type of sevice </summary>
        EnumInfrastructureServicesType ServicesType { get; }

        /// <summary> Unique name for node </summary>
        string NodeName { get; }
    }

    public class NodeInfo : INodeInfo
    {
        public NodeInfo(string nodeName, EnumInfrastructureServicesType servicesType)
        {
            this.NodeName = nodeName;
            this.ServicesType = servicesType;
        }

        public string NodeName { get; }
        public EnumInfrastructureServicesType ServicesType { get; }
    }
}