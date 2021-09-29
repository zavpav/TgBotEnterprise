using System;

namespace CommonInfrastructure
{

    public interface IGlobalEventIdGenerator
    {
        /// <summary> Generate global income message id (NodeType.NodeName.UniqueString) </summary>
        /// <returns></returns>
        /// <remarks>
        /// Need for through looking income information in system.
        /// Used for income telegram meesages, redmine/jenkins events and so on
        /// </remarks>
        string GetNextEventId();
    }


    public class GlobalEventIdGenerator : IGlobalEventIdGenerator
    {
        private readonly INodeInfo _nodeInfo;
        private long _counter;

        public GlobalEventIdGenerator(INodeInfo nodeInfo)
        {
            this._nodeInfo = nodeInfo;
            // TODO need to update from database
            this._counter = 0;
        }

        public string GetNextEventId()
        {
            this._counter++;

            if (string.Compare(this._nodeInfo.ServicesType.ToString(), this._nodeInfo.NodeName, StringComparison.Ordinal) != 0)
                return $"{this._nodeInfo.ServicesType}.{this._nodeInfo.NodeName}.{this._counter}";

            return $"{this._nodeInfo.ServicesType}.#.{this._counter}";

        }
    }
}