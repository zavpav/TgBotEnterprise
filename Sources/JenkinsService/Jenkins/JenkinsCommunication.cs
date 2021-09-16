using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommonInfrastructure;

namespace JenkinsService.Jenkins
{
    public class JenkinsCommunication
    {

        private HttpExtension.AuthInformation? _credential;

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
                var authInfo = JsonSerializer.Deserialize<HttpExtension.AuthInformation>(jsonString);
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
                var configuraton = JsonSerializer.Deserialize<JenkinsConfiguraton>(jsonString);
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