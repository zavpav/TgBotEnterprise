using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using Serilog;

namespace WebAdminService.Data
{
    public class CurrentUsersService
    {
        private readonly IRabbitService _rabbitService;
        private readonly ILogger _logger;
        private readonly IGlobalEventIdGenerator _eventIdGenerator;

        public CurrentUsersService(
            IRabbitService rabbitService, 
            ILogger logger,
            IGlobalEventIdGenerator eventIdGenerator)
        {
            this._rabbitService = rabbitService;
            this._logger = logger;
            this._eventIdGenerator = eventIdGenerator;
        }

        public async Task<UserDataPresentor[]?> GetUsersAsync()
        {
            var request = new RequestAllUsersMessage(this._eventIdGenerator.GetNextIncomeId());
            var response = await this._rabbitService.DirectRequestToMainBot<RequestAllUsersMessage, ResponseAllUsersMessage>(
                    "get-all-users", request);

            var mapperConfig = new AutoMapper.MapperConfiguration(cfg => cfg.CreateMap<ResponseAllUsersMessage.UserInfo, UserDataPresentor>());
            var mapper = mapperConfig.CreateMapper();
            var users = mapper.Map<UserDataPresentor[]>(response.AllUsersInfos) ?? new UserDataPresentor[]{};
            return users;
        }

        public class UserDataPresentor
        {
            /// <summary> Syntethic user id </summary>
            public int UserId { get; set; }
            
            /// <summary> User id in bot-system </summary>
            public string? BotUserId { get; set; }

            /// <summary> User name in bot-system </summary>
            public string? BotUserName { get; set; }

            /// <summary> Autogenerate name in bot-system </summary>
            public string? SystemUserInfo { get; set; }
            
            /// <summary> Jenkins name </summary>
            public string? JenkinsUserName { get; set; }
            
            /// <summary> Redmine full name </summary>
            public string? RedmineUserName { get; set; }

            /// <summary> Is user activated? </summary>
            public bool IsActivate { get; set; }
        }

    }
}