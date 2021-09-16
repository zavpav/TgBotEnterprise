using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommonInfrastructure;
using Microsoft.Extensions.Configuration.Json;
using RedmineService.RabbitCommunication;

namespace RedmineService.Redmine
{
    /// <summary> Communications to Redmine </summary>
    public class RedmineCommunication
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
                var jsonString = await (new System.IO.StreamReader("Secretic/Password.json", Encoding.UTF8).ReadToEndAsync());
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
            var redmineHost = "";
            try
            {
                var jsonString = await (new System.IO.StreamReader("Secretic/Configuration.json", Encoding.UTF8).ReadToEndAsync());
                var configuraton = JsonSerializer.Deserialize<RedmineConfiguraton>(jsonString);
                redmineHost = configuraton?.Host;
            }
            catch (System.IO.FileNotFoundException)
            {
            }
            if (redmineHost == null)
                throw new NotSupportedException("Redmine host undefined");

            var uri = redmineHost + requestPart;
            return await HttpExtension.LoadXmlFromRequest(uri, 
                TimeSpan.FromSeconds(9), 
                this.AddCredential,
                this.UpdateCredential);
        }



        /// <summary> Check redmine alive (select any information from redmine) </summary>
        public async Task<string> GetAnyInformation()
        {
            try
            {
                await this.ExecuteRequest("issues.xml?limit=1");
                return "OK";
            }
            catch (Exception e)
            {
                Console.WriteLine("Redmine: " + e.Message);
                return "Something wrong: " + e.Message;
            }
        }

        public async Task<XDocument> GetLastChangedIssues()
        {
            return await this.ExecuteRequest("issues.xml?status_id=*&sort=updated_on:desc&limit=100");
        }
    }
}