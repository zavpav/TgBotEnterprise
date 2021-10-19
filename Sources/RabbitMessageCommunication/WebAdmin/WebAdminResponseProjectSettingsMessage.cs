using CommonInfrastructure;

namespace RabbitMessageCommunication.WebAdmin
{
    

    /// <summary> Needed service settings </summary>
    public class WebAdminResponseProjectSettingsMessage : IRabbitMessage
    {
        public WebAdminResponseProjectSettingsMessage(string systemEventId, EnumInfrastructureServicesType servicesType,
            string nodeName, string serviceDescription)
        {
            this.SystemEventId = systemEventId;
            this.ServiceDescription = serviceDescription;
            this.ServicesType = servicesType;
            this.NodeName = nodeName;
        }

        /// <summary> Message Income Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Service type </summary>
        public EnumInfrastructureServicesType ServicesType { get; }
        
        /// <summary> Service node name </summary>
        public string NodeName { get; }

        /// <summary> Name of service whom this settings </summary>
        public string ServiceDescription { get; }

        /// <summary> Settings </summary>
        public SettingsItem[] SettingsItems { get; set; } = new SettingsItem[0];

        /// <summary> Single setting </summary>
        public class SettingsItem
        {
            /// <summary> System name of setting </summary>
            public string SystemName { get; set; }
            
            /// <summary> Display name </summary>
            public string Description { get; set; }
            
            /// <summary> Default value of settings </summary>
            public string Value { get; set; }
            
            /// <summary> Type of settings </summary>
            public string SettingType { get; set; }
        }
    }
}