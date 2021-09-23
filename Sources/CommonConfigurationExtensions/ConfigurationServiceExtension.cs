using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMqInfrastructure;
using Serilog;
using Serilog.Events;

namespace CommonInfrastructure
{
    /// <summary> Information and methods about configuration services </summary>
    public static class ConfigurationServiceExtension
    {
        /// <summary> Name for current node </summary>
        public const string CurrentNodeName = "NODE_NAME";

        /// <summary> Address for RabbitMq </summary>
        public const string RabbitMq = "RABBIT_MQ";

        
        public static void ConfigureServices<TDirectRequestProcessor>(IConfiguration configuration,
            IServiceCollection services,
            EnumInfrastructureServicesType servicesType)
            where TDirectRequestProcessor : class, IDirectRequestProcessor
        {
            var currentNode = configuration.GetValue<string?>(CurrentNodeName)
                              ?? servicesType.ToString();
            var nodeInfo = new NodeInfo(currentNode, servicesType);
            services.AddSingleton<INodeInfo>(nodeInfo);

            services.AddSingleton<IGlobalIncomeIdGenerator, GlobalIncomeIdGenerator>();

            var seqLogger = Environment.GetEnvironmentVariable("LOGGER_HOST");
            if (seqLogger != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("InfrastructureType", nodeInfo.ServicesType)
                    .Enrich.WithProperty("InfrastructureNodeName", nodeInfo.NodeName)
                    .WriteTo.Seq(serverUrl: $"http://{seqLogger}:5341", LogEventLevel.Information)
                    .WriteTo.Console()
                    .CreateLogger();
            }
            services.AddTransient<Serilog.ILogger>(p => Log.Logger);


            services.AddTransient<TDirectRequestProcessor>();

            var rabbitHost = configuration.GetValue<string?>(RabbitMq)
                             ?? throw new NotSupportedException("Rabbit host is not definition");

            services.AddSingleton<IRabbitService>(f =>
            {
                var rabbitService = new RabbitService(nodeInfo, 
                    rabbitHost, 
                    f.GetRequiredService<TDirectRequestProcessor>());
                return rabbitService;
            });
        }

        public static void RunApp(IHostBuilder hostBuilder, Action<IHost>? initilizeAction = null)
        {
            var host = hostBuilder.Build();
            host.Services.GetRequiredService<IRabbitService>().Initialize();
            initilizeAction?.Invoke(host);
            host.Run();
        }
    }
}