using System;
using Microsoft.EntityFrameworkCore;
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
            where TDirectRequestProcessor : class, IRabbitProcessor
        {
            services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));

            var currentNode = configuration.GetValue<string?>(CurrentNodeName)
                              ?? servicesType.ToString();
            var nodeInfo = new NodeInfo(currentNode, servicesType);
            services.AddSingleton<INodeInfo>(nodeInfo);

            services.AddSingleton<IGlobalEventIdGenerator, GlobalEventIdGenerator>();

            var seqLogger = Environment.GetEnvironmentVariable("LOGGER_HOST");
            if (seqLogger != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("InfrastructureType", nodeInfo.ServicesType)
                    .Enrich.WithProperty("InfrastructureNodeName", nodeInfo.NodeName)
                    .WriteTo.Seq(serverUrl: $"http://{seqLogger}:5341", LogEventLevel.Information)
                    .WriteTo.Console()
                    .CreateLogger();
            }
            services.AddTransient<Serilog.ILogger>(p => Log.Logger);


            services.AddSingleton<IRabbitProcessor, TDirectRequestProcessor>();

            var rabbitHost = configuration.GetValue<string?>(RabbitMq)
                             ?? throw new NotSupportedException("Rabbit host is not definition");

            services.AddSingleton<IRabbitService>(f =>
            {
                var rabbitService = new RabbitService(nodeInfo, 
                    rabbitHost, 
                    new Lazy<IRabbitProcessor>(f.GetRequiredService<IRabbitProcessor>),
                    f.GetRequiredService<ILogger>());
                return rabbitService;
            });
        }

        public static void RunApp(IHostBuilder hostBuilder, Action<IHost>? initilizeAction = null)
        {
            var host = hostBuilder.Build();
            try
            {
                var requiredService = host.Services.GetRequiredService<IRabbitService>();
                requiredService.Initialize();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Start service error: IRabbitService undef");
                Console.WriteLine(e);
                throw;
            }
            initilizeAction?.Invoke(host);

            try
            {
                var rabbitProcessor = host.Services.GetRequiredService<IRabbitProcessor>();
                rabbitProcessor.Subscribe();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Start service error: IRabbitProcessor undef");
                Console.WriteLine(e);
                throw;
            }

            host.Run();
        }


        public static void ConfigureDatabase<TContext>(this IServiceCollection services, string dbName, IConfiguration configuration)
            where TContext : DbContext
        {
            //services.AddDbContext<TContext>(optionsBuilder =>
            //{
            //    var postgreHost = configuration.GetValue<string?>("POSTGRE_HOST")
            //                      ?? throw new NotSupportedException("Postgre host is not initialized");
            //    var postgrePort = configuration.GetValue<string?>("POSTGRE_PORT")
            //                      ?? throw new NotSupportedException("Postgre port is not initialized");
            //    var postgreUser = configuration.GetValue<string?>("POSTGRE_USER")
            //                      ?? throw new NotSupportedException("Postgre port is not initialized");
            //    var postgrePassword = configuration.GetValue<string?>("POSTGRE_PASSWORD")
            //                          ?? throw new NotSupportedException("Postgre port is not initialized");

            //    Console.WriteLine($"PostgreConnection: {postgreHost}:{postgrePort}");
            //    optionsBuilder
            //        .UseNpgsql($"Host={postgreHost};Port={postgrePort};Database={dbName};Username={postgreUser};Password={postgrePassword}")
            //        //.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Error)
            //        //.EnableSensitiveDataLogging()
            //        .EnableDetailedErrors();
            //});

            services.AddDbContextFactory<TContext>(optionsBuilder =>
            {
                var postgreHost = configuration.GetValue<string?>("POSTGRE_HOST")
                                  ?? throw new NotSupportedException("Postgre host is not initialized");
                var postgrePort = configuration.GetValue<string?>("POSTGRE_PORT")
                                  ?? throw new NotSupportedException("Postgre port is not initialized");
                var postgreUser = configuration.GetValue<string?>("POSTGRE_USER")
                                  ?? throw new NotSupportedException("Postgre port is not initialized");
                var postgrePassword = configuration.GetValue<string?>("POSTGRE_PASSWORD")
                                      ?? throw new NotSupportedException("Postgre port is not initialized");

                Console.WriteLine($"PostgreConnection: {postgreHost}:{postgrePort}");
                optionsBuilder
                    .UseNpgsql($"Host={postgreHost};Port={postgrePort};Database={dbName};Username={postgreUser};Password={postgrePassword}")
                    //.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Error)
                    //.EnableSensitiveDataLogging()
                    //.EnableDetailedErrors()
                    ;
            });

        }

    }
}