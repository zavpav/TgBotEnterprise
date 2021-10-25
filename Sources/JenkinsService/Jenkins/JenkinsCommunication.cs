using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoMapper.Internal;
using CommonInfrastructure;
using JenkinsService.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Serilog;

namespace JenkinsService.Jenkins
{
    public class JenkinsCommunication
    {
        private readonly ILogger _logger;
        private readonly JenkinsDbContext _dbContext;

        private HttpExtension.AuthInformation? _credential;

        public JenkinsCommunication(ILogger logger, JenkinsDbContext dbContext)
        {
            this._logger = logger;
            this._dbContext = dbContext;
        }

        /// <summary> Get project settings </summary>
        /// <param name="projectSysName">Project name</param>
        /// <param name="isFullSettings">if true - fill full job list. if project settings doesn't exist - create stub for it and fill with default values</param>
        public async Task<DbeProjectSettings?> GetProjectSettings(string projectSysName, bool isFullSettings)
        {
            var prj = await this._dbContext.ProjectSettings
                .Include(x => x.JobInformations)
                .SingleOrDefaultAsync(x => x.ProjectSysName == projectSysName);
            
            if (prj == null && isFullSettings)
                prj = new DbeProjectSettings(projectSysName);

            if (prj != null && isFullSettings)
            {
                Enum.GetValues<EnumBuildServerJobs>()
                    .Where(x => x != EnumBuildServerJobs.Undef)
                    .Where(x => prj.JobInformations.All(xx => xx.JobType != x))
                    .Select(x => new DbeProjectSettings.JobDescription
                    {
                        JobType = x,
                        JobPath = ""
                    })
                    .ForEach(x => prj.JobInformations.Add(x));
            }

            return prj;
        }

        /// <summary> Save project settings  </summary>
        public async Task SaveProjectSettings(DbeProjectSettings projectSettings)
        {
            this._dbContext.ProjectSettings.Update(projectSettings);
            await this._dbContext.SaveChangesAsync();
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
    }
}