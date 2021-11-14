using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonInfrastructure;
using MainBotService.Database;
using RabbitMessageCommunication;
using RabbitMessageCommunication.BugTracker;

namespace MainBotService.RabbitCommunication
{
    public interface IMainBotService
    {
        /// <summary> Get All projects info </summary>
        Task<List<DbeProject>> Projects();

        /// <summary> Get tasks from bugtracker </summary>
        /// <param name="projectSysName">Project</param>
        /// <param name="version">Version</param>
        /// <returns>List of issues</returns>
        Task<(string, List<BugTrackerIssue>)> GetBugTrackerIssues(string projectSysName, string? version);

        /// <summary> Get event Id </summary>
        string GetNextEventId();

        /// <summary> Publish message to rabbit </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="actionName">Action name</param>
        /// <param name="outgoingMessage">Message</param>
        /// <param name="subscriberServiceType">serviceType</param>
        Task PublishMessage<T>(string actionName, T outgoingMessage, EnumInfrastructureServicesType? subscriberServiceType = null) where T: IRabbitMessage; 
    }
}