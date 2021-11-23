using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using RabbitMessageCommunication.BuildService;
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

        /// <summary> Get full address for "build" </summary>
        public async ValueTask<string> GetUriForBuild(string jenkinsProjectName, string buildNum)
        {
            return (await this.GetJenkinsHost()) + "job/" + jenkinsProjectName + "/" + buildNum;
        }

        /// <summary> Cache of Jenkins host </summary>
        private string? _jenkinsHost = null;

        /// <summary> Get Jenkins host </summary>
        private async ValueTask<string> GetJenkinsHost()
        {
            if (this._jenkinsHost != null)
                return this._jenkinsHost;

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

            this._jenkinsHost = jenkinsHost ?? throw new NotSupportedException("Jenkins host undefined");
            return this._jenkinsHost;
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
            var uri = (await this.GetJenkinsHost()) + requestPart;
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

        public async Task<List<DtoJobChanged>> UpdateDb()
        {
            try
            {
                var lastBuilds = await this.GetLastBuildFromAllJob();

                var jobChanges = new List<DtoJobChanged>();

                foreach (var build in lastBuilds)
                {
                    var savedBuild = await this._dbContext.JenkinsJobs
                        .FirstOrDefaultAsync(x => 
                            x.BuildNumber == build.BuildNumber
                            && x.JenkinsJobName == build.JenkinsJobName);

                    if (savedBuild == null)
                    {
                        this._logger
                            .ForContext("NewBuild", build, true)
                            .Information("New Jenkins build found for job {jobName}", build.JenkinsJobName);

                        var jc = new DtoJobChanged
                        {
                            OldBuildInfo = null,
                            NewBuildInfo = build,
                            BuildUri = await this.GetUriForBuild(build.JenkinsJobName, build.BuildNumber),
                            ArtifactsUri = ""
                        };

                        jobChanges.Add(jc);

                        this._dbContext.JenkinsJobs.Add(build);
                        continue;
                    }

                    // Check only BuildIsProcessing, BuildStatus for generate DtoJobChanged (other statuses don't matter in "my logic")
                    // BuildDuration also can be changed axiomatically
                    // BuildName, BuildDescription can be changed by use (and sometimes we can lost changes (but it doesn't matter)
                    if (savedBuild.BuildIsProcessing != build.BuildIsProcessing
                        || savedBuild.BuildStatus != build.BuildStatus)
                    {
                        this._logger
                            .ForContext("NewBuild", build, true)
                            .Information("Jenkins build #{buildName} is significantly changed for job {jobName}", 
                                build.BuildNumber,
                                build.JenkinsJobName);

                        var jc = new DtoJobChanged
                        {
                            OldBuildInfo = await this._dbContext.JenkinsJobs.AsNoTracking()
                                .FirstAsync(x => x.BuildNumber == build.BuildNumber),
                            NewBuildInfo = build,
                            BuildUri = await this.GetUriForBuild(build.JenkinsJobName, build.BuildNumber),
                            ArtifactsUri = ""
                        };
                        jobChanges.Add(jc);

                        savedBuild.BuildIsProcessing = build.BuildIsProcessing;
                        savedBuild.BuildStatus = build.BuildStatus;
                    }

                    if (savedBuild.BuildDuration != build.BuildDuration
                        || savedBuild.BuildName != build.BuildName
                        || savedBuild.BuildDescription != build.BuildDescription)
                    {
                        this._logger
                            .ForContext("NewBuild", build, true)
                            .Information("Jenkins build #{buildName} is significantly changed for job {jobName}",
                                build.BuildNumber,
                                build.JenkinsJobName);

                        savedBuild.BuildDuration = build.BuildDuration;
                        savedBuild.BuildName = build.BuildName;
                        savedBuild.BuildDescription = build.BuildDescription;
                    }
                }

                await this._dbContext.SaveChangesAsync();

                // Return information only for tracking jobs
                //jobChanges.RemoveAll(x => x.NewBuildInfo?.ProjectSysName != null);

                return jobChanges;
            }
            catch (Exception e)
            {
                this._logger.Error(e, "Error while updating builds DB");
                throw;
            }
        }

        /// <summary> Disclosed project settings for easy of work </summary>
        private class FlatProjectSettings
        {
            /// <summary> System name of project </summary>
            public string ProjectSysName { get; set; }

            /// <summary> Type of jobs </summary>
            public EnumBuildServerJobs JobType { get; set; }

            /// <summary> Url part </summary>
            public string JobName { get; set; }

        }

        /// <summary> Get all disclosed project settings for easy of work </summary>
        private Task<List<FlatProjectSettings>> GetFlatAllProjectSettings()
        {
            return this
                ._dbContext.ProjectSettings.AsNoTracking()
                .Include(x => x.JobInformations)
                .SelectMany(p =>
                    p.JobInformations.Select(ji =>
                        new FlatProjectSettings
                        {
                            ProjectSysName = p.ProjectSysName,
                            JobName = ji.JobPath,
                            JobType = ji.JobType
                        }
                    ))
                .ToListAsync();
        }



        /// <summary> Get project prefixes are used in git comments (as we can use) </summary>
        private async Task<List<(string projectPrefix, string projectSysName)>> GetProjectPrefixes()
        {
            var allProjectPrefixes = await this._dbContext.ProjectSettings
                .Where(x => x.GitProjectPrefixes != null && x.GitProjectPrefixes != "")
                .Select(x => new {x.ProjectSysName, x.GitProjectPrefixes})
                .ToListAsync();
            var allPrefixes = allProjectPrefixes
                .Where(x => !string.IsNullOrEmpty(x.GitProjectPrefixes))
                .SelectMany(x => 
                    x.GitProjectPrefixes.Split(',')
                            .Select(xx => new {ProjectPrefix = xx.ToLower(), ProjectName = x.ProjectSysName})
                ).ToList();

            if (allPrefixes.Count == 0)
                return new List<(string, string)>();

            return allPrefixes.Select(x => (x.ProjectPrefix, x.ProjectName)).ToList();
        }

        /// <summary> Get all filled builds </summary>
        private async Task<List<DbeJenkinsJob>> GetLastBuildFromAllJob()
        {
            var isFirstLoad = await this._dbContext.JenkinsJobs.AnyAsync();
            var lastBuilds = await this.GetLastClearBuildFromJenkins(isFirstLoad);
            var projectSettings = await this.GetFlatAllProjectSettings();
            var gitPrefixes = await GetProjectPrefixes();
            var reGitComment = new Regex(@"^(?<prj>\S+)\s+#(?<num>\d+)\s+(?<cmmnt>.*)$", RegexOptions.Compiled |  RegexOptions.IgnoreCase);

            // Restore requested fields
            foreach (var build in lastBuilds)
            {
                foreach (var projectSetting in projectSettings)
                {
                    if (build.JenkinsJobName.ToLower() == projectSetting.JobName.ToLower())
                    {
                        build.ProjectSysName = projectSetting.ProjectSysName;
                        build.BuildSubType = projectSetting.JobType;
                    }
                }

                build.BuildStatus = (build.BuildIsProcessing, build.JenkinsBuildStatus.ToUpper()) switch
                {
                    (_, "SUCCESS") => EnumBuildStatus.Success,
                    (_, "FAILURE") => EnumBuildStatus.Failure,
                    (_, "ABORTED") => EnumBuildStatus.Aborted,
                    (_, "UNSTABLE") => EnumBuildStatus.Warning,
                    var (isProcessing, strStatus) => ((Func<EnumBuildStatus>)(() =>
                    {
                        if (isProcessing)
                            return EnumBuildStatus.Processing;


                        this._logger.Warning("Undefined Jenkins build status {buildStatus} {JobName} {buildId}",
                            strStatus,
                            build.JenkinsJobName,
                            build.BuildNumber);
                        return EnumBuildStatus.NotDefined;
                    }))()
                };

                if (build.ChangeInfos != null)
                {
                    foreach (var gitChange in build.ChangeInfos)
                    {
                        foreach (var prefixTuple in gitPrefixes)
                        {
                            if (gitChange.GitComment.ToLower().StartsWith(prefixTuple.projectPrefix))
                            {
                                gitChange.ProjectName = prefixTuple.projectPrefix;
                                gitChange.ProjectSysName = prefixTuple.projectSysName;

                                var reMatch = reGitComment.Match(gitChange.GitComment);
                                if (reMatch.Success)
                                {
                                    gitChange.IssueId = reMatch.Groups["num"].Value;
                                    gitChange.ProjectName = reMatch.Groups["prj"].Value;
                                }

                                break;
                            }
                        }
                    }
                }

            }

            return lastBuilds;
        }

        /// <summary> Get "last" builds from clear Jenkins (does't fill ProjectSysName and some other fields aren't contained in Jenkins) </summary>
        /// <param name="isFirstLoad">Is First load (request many-many builds). (If isn't first load - it loads only 5 last builds for each job)</param>
        private async Task<List<DbeJenkinsJob>> GetLastClearBuildFromJenkins(bool isFirstLoad)
        {
            // /api/xml?tree=jobs[name,builds[number,fullDisplayName,displayName,description,result,timestamp,duration,building,actions[causes[shortDescription,userId,userName]],changeSets[items[comment]]]{0,5}]
            var xmlJenkinsJobs = await this.ExecuteRequest(
                "api/xml?tree=jobs[name,builds[number,fullDisplayName,displayName,description,result,timestamp,duration,building,actions[causes[shortDescription,userId,userName],lastBuiltRevision[branch[*]]],changeSets[items[comment]]]{0,"
                + (isFirstLoad ? 10 : 5) + "}]");

            var xmlHudson = xmlJenkinsJobs.Element("hudson") ?? throw new NotSupportedException("hudson xml root not found");
            var jenkinsJobs = new List<DbeJenkinsJob>();

            foreach (var xJob in xmlHudson.Elements("job"))
            {
                foreach (var xBuild in xJob.Elements("build"))
                {
                    var jenkinsJob = new DbeJenkinsJob
                    {
                        JenkinsJobName = xJob.Element("name")?.Value ?? "",
                        BuildNumber = xBuild.Element("number")?.Value ?? "",
                        BuildName = xBuild.Element("displayName")?.Value ?? "",
                        BuildDescription = xBuild.Element("description")?.Value ?? "",
                        JenkinsBuildStatus = xBuild.Element("result")?.Value ?? "",
                        BuildIsProcessing = bool.Parse(xBuild.Element("building")?.Value ?? "false"),
                        ChangeInfos = new List<DbeJenkinsJob.ChangeInfo>()
                    };

                    var durationText = xBuild.Element("duration")?.Value ?? "0";
                    if (int.TryParse(durationText, out var durationMilliseconds))
                        jenkinsJob.BuildDuration = TimeSpan.FromMilliseconds(durationMilliseconds);

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

                            jenkinsJob.ChangeInfos.Add(gitChange);
                        }
                    }

                    jenkinsJobs.Add(jenkinsJob);
                }
            }

            return jenkinsJobs;
        }

        /// <summary> preAlpha for method of identify project for gitcommits </summary>
        public async Task UpdateGitCommentInfo()
        {
            var allComments = await this._dbContext.Set<DbeJenkinsJob.ChangeInfo>().ToListAsync();
            var gitPrefixes = await this.GetProjectPrefixes();
            var reGitComment = new Regex(@"^(?<prj>\S+)\s+#(?<num>\d+)\s+(?<cmmnt>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (var gitChange in allComments)
            {
                foreach (var prefixTuple in gitPrefixes)
                {
                    if (gitChange.GitComment.ToLower().StartsWith(prefixTuple.projectPrefix))
                    {
                        gitChange.ProjectName = prefixTuple.projectPrefix;
                        gitChange.ProjectSysName = prefixTuple.projectSysName;

                        var reMatch = reGitComment.Match(gitChange.GitComment);
                        if (reMatch.Success)
                        {
                            gitChange.IssueId = reMatch.Groups["num"].Value;
                            gitChange.ProjectName = reMatch.Groups["prj"].Value;
                        }

                        break;
                    }
                }
            }

            await this._dbContext.SaveChangesAsync();
        }
    }
}