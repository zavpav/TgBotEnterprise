using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoMapper;
using CommonInfrastructure;
using RedmineService.Database;
using Serilog;

namespace RedmineService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigurationServiceExtension.RunApp(CreateHostBuilder(args));
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    ConfigurationServiceExtension.ConfigureServices<RabbitProcessor>(configuration, services,
                        EnumInfrastructureServicesType.BugTracker);

                    services.ConfigureDatabase<RedmineDbContext>("redmine", configuration);

                    var mapperConfig = new MapperConfiguration(mc =>
                    {
                        mc.AddProfile(new MappingProfile());
                    });
                    var mapper = mapperConfig.CreateMapper();
                    services.AddSingleton<IMapper>(mapper);

                    services.AddHostedService<RedmineWorker>();
                });
    }
}
