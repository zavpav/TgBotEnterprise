using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMessageCommunication.WebAdmin;
using RabbitMqInfrastructure;
using Serilog;

namespace MainBotService.RabbitCommunication
{
    public class DirectRequestProcessor : IDirectRequestProcessor
    {
        private readonly INodeInfo _nodeInfo;
        private readonly ILogger _logger;

        public DirectRequestProcessor(INodeInfo nodeInfo, ILogger logger)
        {
            this._nodeInfo = nodeInfo;
            this._logger = logger;
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

            if (actionName.ToUpper() == "GET-ALL-USERS")
            {
                var requestAllMessage = JsonSerializer.Deserialize<RequestAllUsersMessage>(directMessage);
                this._logger.Information(requestAllMessage, "Processing {actionName} message {@message}", actionName, requestAllMessage);

                var responseMessage = new ResponseAllUsersMessage(requestAllMessage.SystemEventId)
                {
                    AllUsersInfos = new [] {new ResponseAllUsersMessage.UserInfo
                    {
                        UserId = 1,
                        BotUserId = "None",
                        BotUserName = null,
                        IsActivate = false,
                        SystemUserInfo = "SomeInformation from telegramm"
                    }
                    }
                };

                
                this._logger.Information(responseMessage, "Response {@response}", responseMessage);
                return JsonSerializer.Serialize(responseMessage);
            }

            await Task.Delay(9000);
            Console.WriteLine($"{this._nodeInfo.NodeName} - {actionName} - {directMessage}");
            return directMessage;
        }
    }
}