using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommonInfrastructure;
using Serilog;

namespace JenkinsService.Jenkins
{
    public class JenkinsCommunication
    {
        private readonly ILogger _logger;

        private HttpExtension.AuthInformation? _credential;

        public JenkinsCommunication(ILogger logger)
        {
            this._logger = logger;
        }

        private Task AddCredential(WebRequest request)
        {
            if (this._credential == null)
                return Task.CompletedTask;

            request.UpdateCredential(this._credential);

            return Task.CompletedTask;
        }

        private async Task UpdateCredential()
        {
            try
            {
                var jsonString = await(new System.IO.StreamReader("Secretic/Password.json", Encoding.UTF8).ReadToEndAsync());
                var authInfo = JsonSerializer2.DeserializeRequired<HttpExtension.AuthInformation>(jsonString, this._logger);
                this._credential = authInfo;
            }
            catch (System.IO.FileNotFoundException)
            {
            }
        }

        /// <summary> Execute query. Add main url part. Configure credintals and etc </summary>
        private async Task<XDocument> ExecuteRequest(string requestPart)
        {
            var jenkinsHost = "";
            try
            {
                var jsonString = await (new System.IO.StreamReader("Secretic/Configuration.json", Encoding.UTF8).ReadToEndAsync());
                var configuraton = JsonSerializer2.DeserializeRequired<JenkinsConfiguraton>(jsonString, this._logger);
                jenkinsHost = configuraton?.Host;
            }
            catch (System.IO.FileNotFoundException)
            {
            }
            if (jenkinsHost == null)
                throw new NotSupportedException("Jenkins host undefined");


            var uri = jenkinsHost + requestPart;
            return await HttpExtension.LoadXmlFromRequest(uri,
                TimeSpan.FromSeconds(9),
                this.AddCredential,
                this.UpdateCredential);
        }

        public async Task<string> GetAnyInformation()
        {
            try
            {
                await this.ExecuteRequest("api/xml");
                return "OK";
            }
            catch (Exception e)
            {
                Console.WriteLine("Redmine: " + e.Message);
                return "Redmine something wrong: " + e.Message;
            }
        }

    }
}