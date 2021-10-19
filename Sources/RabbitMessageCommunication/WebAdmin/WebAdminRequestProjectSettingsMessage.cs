namespace RabbitMessageCommunication.WebAdmin
{
    /// <summary> Request project setting for project from all services by WebAdmin </summary>
    public class WebAdminRequestProjectSettingsMessage : IRabbitMessage
    {
        public WebAdminRequestProjectSettingsMessage(string systemEventId, string projectSysName)
        {
            this.SystemEventId = systemEventId;
            this.ProjectSysName = projectSysName;
        }

        /// <summary> Message Income Id - Unique id in all services </summary>
        public string SystemEventId { get; }

        /// <summary> SysName of requested project </summary>
        public string ProjectSysName { get; }
    }
}