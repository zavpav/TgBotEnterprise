using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommonInfrastructure;
using JenkinsService.Database;
using JenkinsService.Jenkins;
using JenkinsService.RabbitCommunication;
using Serilog;

namespace JenkinsService
{
    public static class Program
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
                        EnumInfrastructureServicesType.BuildService);

                    services.ConfigureDatabase<JenkinsDbContext>("jenkins", configuration);

                    var mapperConfig = new MapperConfiguration(mc =>
                    {
                        mc.AddProfile(new MappingProfile());
                    });
                    var mapper = mapperConfig.CreateMapper();
                    services.AddSingleton<IMapper>(mapper);

                    services.AddSingleton<JenkinsCommunication>();

                    services.AddHostedService<JenkinsWorker>();
                });
    }
}
