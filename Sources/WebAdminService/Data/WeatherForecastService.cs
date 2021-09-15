using System;
using System.Linq;
using System.Threading.Tasks;
using CommonInfrastructure;
using RabbitMqInfrastructure;

namespace WebAdminService.Data
{
    public class WeatherForecastService
    {
        private readonly IRabbitService _rabbitService;

        public WeatherForecastService(IRabbitService rabbitService)
        {
            _rabbitService = rabbitService;
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
        {
            var rng = new Random();
            return Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }).ToArray());
        }

        public async Task<string> RabbitRequest()
        {
           return await this._rabbitService.DirectRequest(EnumInfrastructureServicesType.BugTracker, "WhatsNew", "whatsnew");
        }
    }
}
