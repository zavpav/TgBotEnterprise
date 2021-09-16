using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMqInfrastructure;

namespace WebAdminService.Data
{
    public class BugTrackerService
    {
        private readonly IRabbitService _rabbitService;

        public BugTrackerService(IRabbitService rabbitService)
        {
            this._rabbitService = rabbitService;
        }


        public async Task<string> GetAnyData()
        {
            return await this._rabbitService.DirectRequest(EnumInfrastructureServicesType.BugTracker, "ANY_QUERY", "any");
        }
    }
}