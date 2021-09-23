using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using CommonInfrastructure;
using Serilog;

namespace WebAdminService
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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
