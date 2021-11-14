using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommonInfrastructure;
using JenkinsService.Database;
using Microsoft.EntityFrameworkCore;
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
                this.UpdateCredential,
                this._logger);
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

        private readonly Regex _reGitComment = new Regex(@"^(?<prj>\S+)\s+#(?<num>\d+)\s+(?<cmmnt>.*)$");

        public async Task<List<DtoJobChanged>> UpdateDb()
        {
            var isFirstLoad = await this._dbContext.JenkinsJobs.AnyAsync();
            // /api/xml?tree=jobs[name,builds[number,fullDisplayName,displayName,description,result,timestamp,duration,building,actions[causes[shortDescription,userId,userName]],changeSets[items[comment]]]{0,5}]
            var xmlJenkinsJobs = await this.ExecuteRequest("api/xml?tree=jobs[name,builds[number,fullDisplayName,displayName,description,result,timestamp,duration,building,actions[causes[shortDescription,userId,userName],lastBuiltRevision[branch[*]]],changeSets[items[comment]]]{0," 
                                                           +  (isFirstLoad ? 10 : 5) + "]");

            var xmlHudson = xmlJenkinsJobs.Element("hubson") ?? throw new NotSupportedException("hudson xml root not found");
            var jenkinsJobs = new List<DbeJenkinsJob>();

            foreach (var xJob in xmlHudson.Elements("job"))
            {
                foreach (var xBuild in xJob.Elements("build"))
                {
                    var jenkinsJob = new DbeJenkinsJob
                    {
                        JenkinsProjectName = xJob.Element("name")?.Value ?? "",
                        BuildNumber = xBuild.Element("number")?.Value ?? "",
                        BuildName = xBuild.Element("displayName")?.Value ?? "",
                        BuildDescription = xBuild.Element("description")?.Value ?? "",
                        BuildStatus = xBuild.Element("result")?.Value ?? "",
                        BuildIsProcessing = bool.Parse(xBuild.Element("building")?.Value ?? "false"),
                        ChangeInfos = new List<DbeJenkinsJob.ChangeInfo>()
                    };

                    // "Other" information about build
                    foreach (var xAction in xBuild.Elements("action"))
                    {
                        var className = xAction.Attribute("_class")?.Value ?? "";
                        if (className == "hudson.plugins.git.util.BuildData")
                        {
                            jenkinsJob.BuildBranchName = xAction.Element("lastBuiltRevision")?
                                                             .Element("branch")?
                                                             .Element("name")?
                                                             .Value 
                                                         ?? "";
                        }
                        else if (className == "hudson.model.CauseAction")
                        {
                            jenkinsJob.JenkinsBuildStarter = xAction.Element("cause")?
                                                                 .Element("shortDescription")?
                                                                 .Value
                                                             ?? "";

                        }
                    }

                    var changeSetComments = xBuild
                        .Element("changeSet")?
                        .Elements("item")
                        .Select(x => x.Element("comment")?.Value ?? "");
                    if (changeSetComments != null)
                    {
                        foreach (var jenComment in changeSetComments)
                        {
                            if (string.IsNullOrEmpty(jenComment))
                                continue;

                            var gitChange = new DbeJenkinsJob.ChangeInfo
                            {
                                GitComment = jenComment
                            };

                            var reMatch = this._reGitComment.Match(jenComment);
                            if (reMatch.Success)
                            {
                                gitChange.IssueId = reMatch.Groups["num"].Value;
                                gitChange.ProjectName = reMatch.Groups["prj"].Value;
                            }

                            jenkinsJob.ChangeInfos.Add(gitChange);
                        }
                    }

                    jenkinsJobs.Add(jenkinsJob);
                }
            }

            //jenkinsJobs

            return new List<DtoJobChanged>();
        }
        
    }
}