using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using CommonInfrastructure;
using MainBotService.Database;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using Serilog;

namespace MainBotService.RabbitCommunication
{
    public class DirectRequestProcessor : IDirectRequestProcessor
    {
        private readonly INodeInfo _nodeInfo;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly BotServiceDbContext _dbContext;

        public DirectRequestProcessor(INodeInfo nodeInfo, ILogger logger, IMapper mapper, BotServiceDbContext dbContext)
        {
            this._nodeInfo = nodeInfo;
            this._logger = logger;
            this._mapper = mapper;
            this._dbContext = dbContext;
        }

        public async Task<string> ProcessDirectUntypedMessage(IRabbitService rabbit, 
            string actionName, 
            IDictionary<string, string> messageHeaders,
            string directMessage)
        {
            this._logger.Information("Untrack Info Direct request {actionName} message data {directMessage}", actionName, directMessage);
            if (actionName.ToUpper() == "PING")
            {
                return "OK"; // Check only existing service
            }

            if (actionName.ToUpper() == RabbitMessages.MainBotDirectGetAllUsers.ToUpper())
            {
                var requestAllMessage = JsonSerializer2.DeserializeRequired<RequestAllUsersMessage>(directMessage, this._logger);
                this._logger.Information(requestAllMessage, "Processing {actionName} message {@message}", actionName, requestAllMessage);

                var allUsers = await this._dbContext.UsersInfo.ToListAsync();
                var allUsersPack = this._mapper.Map<ResponseAllUsersMessage.UserInfo[]>(allUsers.ToArray()) 
                                   ?? new ResponseAllUsersMessage.UserInfo[0];

                var responseMessage = new ResponseAllUsersMessage(requestAllMessage.SystemEventId) { AllUsersInfos = allUsersPack };

                
                this._logger.Information(responseMessage, "Response {@response}", responseMessage);
                return JsonSerializer.Serialize(responseMessage);
            }

            await Task.Delay(9000);
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");
            return directMessage;
        }
    }
}