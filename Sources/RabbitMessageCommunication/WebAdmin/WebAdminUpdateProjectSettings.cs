using CommonInfrastructure;

namespace RabbitMessageCommunication.WebAdmin
{
    /// <summary> Update peoject setting for service </summary>
    public class WebAdminUpdateProjectSettings : IRabbitMessage
    {
        public WebAdminUpdateProjectSettings(string systemEventId, EnumInfrastructureServicesType servicesType,
            string nodeName, SettingsItem[] settingsItems)
        {
            this.SystemEventId = systemEventId;
            this.SettingsItems = settingsItems;
            this.ServicesType = servicesType;
            this.NodeName = nodeName;
        }

        /// <summary> Message Income Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> Service type </summary>
        public EnumInfrastructureServicesType ServicesType { get; }

        /// <summary> Service node name </summary>
        public string NodeName { get; }

        /// <summary> Settings </summary>
        public SettingsItem[] SettingsItems { get; }

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