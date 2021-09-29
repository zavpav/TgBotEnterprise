using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using RabbitMessageCommunication;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using Serilog;

namespace WebAdminService.Data
{
    public class CurrentUsersService
    {
        private readonly IRabbitService _rabbitService;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IGlobalEventIdGenerator _eventIdGenerator;

        public CurrentUsersService(
            IRabbitService rabbitService, 
            ILogger logger,
            IMapper mapper,
            IGlobalEventIdGenerator eventIdGenerator)
        {
            this._rabbitService = rabbitService;
            this._logger = logger;
            _mapper = mapper;
            this._eventIdGenerator = eventIdGenerator;
        }

        public async Task<UserDataPresentor[]?> GetUsersAsync()
        {
            var request = new RequestAllUsersMessage(this._eventIdGenerator.GetNextEventId());
            var response = await this._rabbitService.DirectRequestToMainBot<RequestAllUsersMessage, ResponseAllUsersMessage>(
                RabbitMessages.WebGetAllUsers, request);

            var users = this._mapper.Map<UserDataPresentor[]>(response.AllUsersInfos) ?? new UserDataPresentor[]{};
            return users;
        }

        public class UserDataPresentor
        {
            /// <summary> Syntethic user id </summary>
            public int UserId { get; set; }
            
            /// <summary> User id in bot-system </summary>
            public string? BotUserId { get; set; }

            /// <summary> User name in bot-system </summary>
            public string? WhoIsThis { get; set; }

            /// <summary> Jenkins name </summary>
            public string? JenkinsUserName { get; set; }
            
            /// <summary> Redmine full name </summary>
            public string? RedmineUserName { get; set; }

            /// <summary> Is user activated? </summary>
            public bool IsActive { get; set; }
        }

    }
}