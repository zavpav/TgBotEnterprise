using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using RedmineService.Database;
using Serilog;

namespace RedmineService.Redmine
{
    /// <summary> Communications to Redmine </summary>
    public class RedmineCommunication
    {
        private readonly ILogger _logger;
        private readonly RedmineDbContext _dbContext;
        private HttpExtension.AuthInformation? _credential;

        public RedmineCommunication(ILogger logger, RedmineDbContext dbContext)
        {
            this._logger = logger;
            this._dbContext = dbContext;
        }

        /// <summary> Execute query. Add main url part. Configure credintals and etc </summary>
        private async Task<XDocument> ExecuteRequest(string requestPart)
        {
            var redmineHost = "";
            try
            {
                var jsonString = await (new System.IO.StreamReader("Secretic/Configuration.json", Encoding.UTF8).ReadToEndAsync());
                var configuraton = JsonSerializer2.DeserializeRequired<RedmineConfiguraton>(jsonString, this._logger);
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


        /// <summary> Get project settings </summary>
        /// <param name="projectSysName">Project name</param>
        /// <param name="isFullSettings">if true - fill full job list. if project settings doesn't exist - create stub for it and fill with default values</param>
        public async Task<DbeProjectSettings?> GetProjectSettings(string projectSysName, bool isFullSettings)
        {
            var prj = await this._dbContext.ProjectSettings.SingleOrDefaultAsync(x => x.ProjectSysName == projectSysName);
            
            if (prj == null && isFullSettings)
                prj = new DbeProjectSettings(projectSysName);

            return prj;
        }

        /// <summary> Save project settings  </summary>
        public async Task SaveProjectSettings(DbeProjectSettings projectSettings)
        {
            this._dbContext.ProjectSettings.Update(projectSettings);
            await this._dbContext.SaveChangesAsync();
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
                var jsonString = await (new System.IO.StreamReader("Secretic/Password.json", Encoding.UTF8).ReadToEndAsync());
                var authInfo = JsonSerializer2.DeserializeRequired<HttpExtension.AuthInformation>(jsonString, this._logger);
                this._credential = authInfo;
            }
            catch (System.IO.FileNotFoundException)
            {
            }
        }


        public async Task<RedmineIssue[]> SimpleFindIssues(string? userBotId, string? projectSysName, string? versionText)
        {
            await Task.Yield();
            return new RedmineIssue[]
            {
                new RedmineIssue
                {
                    Num = "12",
                    AssignOn = "Пфафа",
                    CreatorName = "фывафыва",
                    Description = "Вуыск ",
                    ProjectName = "АСУБС",
                    ProjectSysName = "Fbpf",
                    Resolution = "нене ",
                    Status = "Черновик",
                    Subject = "Subs",
                    Version = "100"
                },
                new RedmineIssue
                {
                    Num = "123",
                    AssignOn = "Пфаф23fа",
                    CreatorName = "фываf23фыва",
                    Description = "Вуf323fыск ",
                    ProjectName = "АСУБС",
                    ProjectSysName = "Fbpf",
                    Resolution = "н23f32fене ",
                    Status = "Черновик",
                    Subject = "Su2f3f2323fbs",
                    Version = "101"
                },
            };
        }
    }
}