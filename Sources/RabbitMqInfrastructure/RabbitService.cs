using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace RabbitMqInfrastructure
{
    public class RabbitService : IRabbitService//, IDisposable
    {
        private const string SystemEventIdHeaderName = "SystemEventId";

        /// <summary> Information about current node </summary>
        private readonly INodeInfo _nodeInfo;

        /// <summary> Rabbit server </summary>
        private readonly string _rabbitHost;

        private readonly IDirectRequestProcessor _requestProcessor;
        private readonly ILogger _logger;


        public RabbitService(INodeInfo nodeInfo,
            string rabbitHost, 
            IDirectRequestProcessor requestProcessor,
            ILogger logger)
        {
            this._nodeInfo = nodeInfo;
            this._rabbitHost = rabbitHost;
            this._requestProcessor = requestProcessor;
            this._logger = logger;
        }


        /// <summary> Queue name for direct exchange  </summary>
        private static readonly string DirectRequestExchangeName = RequestPrefixes.DirectRequest;

        /// <summary> Name for publish data from node </summary>
        private static readonly string CentralHubExchangeName = RequestPrefixes.CentralPublisher;

        private volatile IConnection? _connection;
        private IModel? _channel;
        
        /// <summary> Connection to rabbit </summary>
        private IConnection Connection()
        { 
            this.Initialize();
            return this._connection ?? throw new NotSupportedException("NotInitialized");
        }

        private IModel Channel()
        {
            this.Initialize();
            return this._channel ?? throw new NotSupportedException("NotInitialized");
        }

        /// <summary> Initialize class </summary>
        /// <remarks>
        /// Rabbit recommend to keep connections
        /// </remarks>
        public void Initialize()
        {
            if (this._connection == null)
            {
                lock (this)
                {
                    if (this._connection == null)
                    {
                        #region Create communication
                        var rabbitFactory = new ConnectionFactory
                        {
                            HostName = this._rabbitHost,
                            DispatchConsumersAsync = true
                        };

                        this._connection = rabbitFactory.CreateConnection();
                        this._channel = this._connection.CreateModel();
                        #endregion

                        #region Rabbit configuration
                        // Configure exchange for looking direct messages
                        this._channel.ExchangeDeclare(exchange: DirectRequestExchangeName, type: ExchangeType.Headers);

                        var directQueue = this._channel.QueueDeclare($"{RequestPrefixes.DirectRequest}.{this._nodeInfo.ServicesType}.{this._nodeInfo.NodeName}",
                            durable: true,
                            exclusive: false,
                            autoDelete: false);

                        var headers = new Dictionary<string, object>
                        {
                            { "QueueType", RequestPrefixes.DirectRequest },
                            { "InfrastructureServicesType", this._nodeInfo.ServicesType.ToString() }
                        };

                        this._channel.QueueBind(directQueue.QueueName,
                            DirectRequestExchangeName,
                            routingKey:string.Empty,
                            headers);

                        // Configure exchange for publish information from node
                        this._channel.ExchangeDeclare(exchange: CentralHubExchangeName, type: ExchangeType.Headers);

                        #endregion

                        #region Configure direct message processing

                        var directMessageConsumer = new AsyncEventingBasicConsumer(this._channel);
                        directMessageConsumer.Received += async (s, e) => { await this.ProcessDirectRequest(e); };

                        this._channel.BasicConsume(queue: directQueue.QueueName,
                            autoAck: true,
                            consumer: directMessageConsumer);

                        #endregion
                    }
                }
            }
        }

        /// <summary> Process direct request </summary>
        private async Task ProcessDirectRequest(BasicDeliverEventArgs e)
        {
            var requestMessage = Encoding.UTF8.GetString(e.Body.ToArray());
            var messageHeaders = this.ConvertHeadersToString(e.BasicProperties.Headers);
            
            messageHeaders.TryGetValue(SystemEventIdHeaderName, out var eventId);
            this._logger.InformationWithEventContext(eventId, "ProcessDirectRequest {rawMessage} {@headers}", requestMessage, messageHeaders);


            var actionName = messageHeaders["ActionName"];
            var responseMessage = await this._requestProcessor.ProcessDirectUntypedMessage(this, actionName, messageHeaders, requestMessage);

            this._logger.InformationWithEventContext(eventId, "Response ProcessDirectRequest {rawMessage}", responseMessage);

            var msgBody = Encoding.UTF8.GetBytes(responseMessage);
            this._channel.BasicPublish("", e.BasicProperties.CorrelationId, body: msgBody);
        }

        /// <summary> Direct request for another service </summary>
        /// <param name="serviceType">Service type</param>
        /// <param name="actionName">Method name</param>
        /// <param name="message">Message</param>
        /// <param name="eventId">Unique event id</param>
        public Task<string> DirectRequest(EnumInfrastructureServicesType serviceType, string actionName, string message, string? eventId = null)
        {
            var responseQueueName = $"{RequestPrefixes.Response}.{this._nodeInfo.NodeName}.{Guid.NewGuid()}";

            var headers = new Dictionary<string, object>
            {
                { "QueueType", RequestPrefixes.DirectRequest },
                { "InfrastructureServicesType", serviceType.ToString() },
                { "ActionName",  actionName },
                { "ResponseQueue",  responseQueueName }
            };
            if (eventId != null)
                headers.Add(SystemEventIdHeaderName, eventId);

            this._logger.InformationWithEventContext(eventId, "DirectRequest {rawMessage} {@headers}", message, headers);
            var responseTask = this.ResponseTaskSource(responseQueueName, eventId);

            this.DirectRequest(message, responseQueueName, headers);

            return responseTask;
        }

        /// <summary> Generate "Task" for waiting response </summary>
        private Task<string> ResponseTaskSource(string responseQueueName, string eventId = null)
        {
            var channel = this.Channel();

            var tcsResponse = new TaskCompletionSource<string>();
            var responseQueue = channel.QueueDeclare(responseQueueName,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null);

            var responseConsumer = new AsyncEventingBasicConsumer(channel);
            responseConsumer.Received += (s, e) =>
            {
                var responseMessage = Encoding.UTF8.GetString(e.Body.ToArray());
                this._logger.InformationWithEventContext(eventId, "Response task {response}", responseMessage);
                channel.QueueDelete(responseQueueName);
                tcsResponse.SetResult(responseMessage);
                return Task.CompletedTask;
            };

            channel.BasicConsume(queue: responseQueue.QueueName,
                autoAck: true,
                consumer: responseConsumer);


            return ResponseTaskSourceWait(tcsResponse, channel, responseQueue.QueueName);
        }

        private async Task<string> ResponseTaskSourceWait(TaskCompletionSource<string> tcsResponse,
            IModel channel, string responseQueueName)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {

                var completedTask = await Task.WhenAny(tcsResponse.Task, Task.Delay(TimeSpan.FromSeconds(10), timeoutCancellationTokenSource.Token));
                if (completedTask == tcsResponse.Task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await tcsResponse.Task;  
                }
                else
                {
                    channel.QueueDelete(responseQueueName);
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        /// <summary> Publish information from node to CentralHub </summary>
        /// <param name="actionName">Method name</param>
        /// <param name="message">Information</param>
        /// <param name="eventId">Unique event id</param>
        public Task PublishInformation(string actionName, string message, string? eventId = null)
        {
            var channel = this.Channel();

            var publishHeaders = new Dictionary<string, object>
            {
                { "QueueType", RequestPrefixes.CentralPublisher },
                { "Publisher", this._nodeInfo.ServicesType.ToString() },
                { "ActionName", actionName }
            };
            if (eventId != null)
                publishHeaders.Add(SystemEventIdHeaderName, eventId);

            this._logger.InformationWithEventContext(eventId, "PublishInformation {rawMessage} {@headers}", message, publishHeaders);


            var basicProperties = channel.CreateBasicProperties();
            basicProperties.Headers = publishHeaders;

            var informationBody = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: CentralHubExchangeName,
                routingKey: string.Empty,
                body: informationBody,
                basicProperties: basicProperties);

            return Task.CompletedTask;
        }

        /// <summary> Subscribe to central information </summary>
        /// <param name="serviceType">Service type</param>
        /// <param name="actionName">Action. If null - subcribe all for service</param>
        /// <param name="processFunc">Func generate Task for processing message params[message, headers]</param>
        public void Subscribe(EnumInfrastructureServicesType serviceType, string? actionName, ProcessMessage processFunc)
        {
            var channel = this.Channel();

            var subscribedQueue = channel.QueueDeclare(
                $"{RequestPrefixes.IncomeData}.{this._nodeInfo.ServicesType}.{this._nodeInfo.NodeName}.{actionName ?? "#"}",
                durable: true,
                exclusive: false,
                autoDelete: false);

            var headers = new Dictionary<string, object>
            {
                { "QueueType", RequestPrefixes.CentralPublisher },
                { "Publisher", serviceType.ToString() }
            };

            if (actionName != null )
                headers.Add("ActionName", actionName);

            channel.QueueBind(subscribedQueue.QueueName,
                CentralHubExchangeName,
                routingKey: string.Empty,
                headers);

            var subcribeConsumer = new AsyncEventingBasicConsumer(this._channel);
            subcribeConsumer.Received += async (s, e) =>
            {
                var publishedInformation = Encoding.UTF8.GetString(e.Body.ToArray());
                var dicHeaders = this.ConvertHeadersToString(e.BasicProperties.Headers);
                dicHeaders.TryGetValue(SystemEventIdHeaderName, out var eventId);
                this._logger.InformationWithEventContext(eventId, "Process Subscribe message {message} {@headers}", publishedInformation, dicHeaders);
                await processFunc(publishedInformation, dicHeaders);
            };

            channel.BasicConsume(queue: subscribedQueue.QueueName,
                autoAck: true,
                consumer: subcribeConsumer);

        }

        /// <summary> Directly sending message to another node </summary>
        /// <param name="message">Message</param>
        /// <param name="responseQueueName">Queue name for response</param>
        /// <param name="requestHeaders">Information for find other service and execute request</param>
        private void DirectRequest(string message, string responseQueueName, Dictionary<string, object> requestHeaders)
        {
            var channel = this.Channel();

            var requestBody = Encoding.UTF8.GetBytes(message);

            var basicProperties = channel.CreateBasicProperties();
            basicProperties.CorrelationId = responseQueueName;
            basicProperties.Headers = requestHeaders;

            channel.BasicPublish(exchange: DirectRequestExchangeName,
                routingKey: string.Empty,
                body: requestBody,
                basicProperties: basicProperties);
        }

        /// <summary> Convert messageHeaders to Dictionary[string, string] </summary>
        private Dictionary<string, string> ConvertHeadersToString(IDictionary<string, object> messageHeaders)
        {
            var dic = new Dictionary<string, string>();

            foreach (var valuePair in messageHeaders)
            {
                var val = Encoding.UTF8.GetString((byte[])valuePair.Value);
                dic.Add(valuePair.Key, val);
            }

            return dic;
        }
    }
}