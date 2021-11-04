using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
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

        /// <summary> Get issues by "simple filter" </summary>
        /// <param name="userBotId">User botId</param>
        /// <param name="projectSysName">Name of project in bot system</param>
        /// <param name="versionText">Version name as text (required projectSysName)</param>
        /// <param name="statusesNames">Statuses</param>
        public async Task<List<RedmineIssue>> SimpleFindIssues(string? userBotId,
            string? projectSysName,
            string? versionText, 
            string[]? statusesNames)
        {
            await Task.Yield();

            var issuesRequest = "issues.xml?sort=updated_on:desc&limit=1000";

            if (!string.IsNullOrEmpty(userBotId))
            {
                //assigned_to
                var remineUserId = await this.GetRedmineUserId(userBotId);
                if (remineUserId != null)
                    issuesRequest += $"&assigned_to_id={remineUserId}";
                else
                    this._logger.Error("Find redmine user id error. Id = null");
            }

            if (statusesNames != null)
            {
                var allStatuses = new List<int>();
                foreach (var statusesName in statusesNames)
                {
                    var statusId = await this.GetStatusId(statusesName);
                    if (statusId == null)
                        this._logger.Error("Find redmine status id error. Id = null {statusesName}", statusesName);
                    else
                        allStatuses.Add(statusId.Value);
                }
                if (allStatuses.Count != 0)
                    issuesRequest += "&status_id=" + string.Join(";", allStatuses);
            }

            if (!string.IsNullOrEmpty(projectSysName))
            {
                var projectId = await this.GetProjectId(projectSysName);
                if (projectId != null)
                    issuesRequest += $"&project_id={projectId}";
                else
                    this._logger.Error("Find redmine project id error. Id = null");
            }

            if (!string.IsNullOrEmpty(versionText))
            {
                if (string.IsNullOrEmpty(projectSysName))
                {
                    this._logger.Error("Request data by version without project information {versionText}", versionText);
                }
                else
                {
                    var versionId = await this.GetVersionId(projectSysName, versionText);
                    if (versionId != null)
                        issuesRequest += $"&fixed_version={versionId}";
                    else
                        this._logger.Error("Find redmine version id error. Id = null");
                }
            }

            var redmineIssues = new List<RedmineIssue>();

            var xIssues = (await this.ExecuteRequest(issuesRequest)).Element("issues");
            if (xIssues != null)
            {
                var findUserCache = new ConcurrentDictionary<string, string?>();
                foreach (var xIssue in xIssues.Elements("issue"))
                {
                    try
                    {
                        redmineIssues.Add(await this.CreateIssueByXmlElement(xIssue, findUserCache));
                    }
                    catch (Exception e)
                    {
                        this._logger.Error(e, "Create issue error");
                        throw;
                    }
                }
            }
            else
            {
                this._logger.Information("Issues not found. Request {issuesRequest}", issuesRequest);
            }

            return redmineIssues;
        }

        /// <summary> Create single RedmineIssue by xml-information </summary>
        /// <param name="xIssue">Xml-issue</param>
        /// <param name="findUserCache">Cache for finding userBotId</param>
        private async Task<RedmineIssue> CreateIssueByXmlElement(XElement xIssue, ConcurrentDictionary<string, string?> findUserCache)
        {
            var issue = new RedmineIssue
            {
                Num = xIssue.Element("id")?.Value ?? "<not defined>",
                Subject = xIssue.Element("subject")?.Value ?? "",
                Description = xIssue.Element("description")?.Value ?? "",
                Status = xIssue.Element("status")?.Attribute("name")?.Value ?? "<not defined>",
                Version = xIssue.Element("fixed_version")?.Attribute("name")?.Value ?? "",
                CreatorName = xIssue.Element("author")?.Attribute("name")?.Value ?? "",
                AssignOn = xIssue.Element("assigned_to")?.Attribute("name")?.Value ?? ""
            };

            var resolution = xIssue.Element("custom_fields")
                ?.Elements("custom_field")
                .FirstOrDefault(x => x.Attribute("name")?.Value == "Резолюция")
                ?.Value;
            if (!string.IsNullOrEmpty(resolution))
                issue.Resolution = resolution;

            // maybe find human-name
            var redmineProjectName = xIssue.Element("project")?.Attribute("name")?.Value ?? "";
            issue.ProjectName = redmineProjectName;

            var projectSetting = await this._dbContext.ProjectSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.RedmineProjectName == redmineProjectName);
            issue.ProjectSysName = projectSetting?.ProjectSysName ?? "<not defined>";

            var updateOnStr = xIssue.Element("updated_on")?.Value;
            if (updateOnStr != null)
            {
                //2020-03-18T12:48:35Z
                var dt = DateTime.ParseExact(updateOnStr, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                issue.UpdateOn = dt;
            }

            if (!findUserCache.TryGetValue(issue.AssignOn, out var botUserId))
            {
                var userInfo = await this._dbContext.UsersInfo.AsNoTracking().FirstOrDefaultAsync(x => x.RedmineName == issue.AssignOn);
                var userBotId = userInfo?.RedmineName;
                findUserCache.TryAdd(issue.AssignOn, userBotId);
                issue.AssignOnUserBotId = userBotId;
            }

            return issue;
        }

        /// <summary> Get user id by botId </summary>
        /// <param name="userBotId">Bot user id</param>
        /// <returns>User id in redmine</returns>
        private async Task<int?> GetRedmineUserId(string userBotId)
        {
            var userInfo = await this._dbContext.UsersInfo.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BotUserId == userBotId);
            if (userInfo == null)
            {
                this._logger.Error("Find user info error by userBotId= ({userBotId})", userBotId);
                return null;
            }
            if (userInfo.RedmineUserId != null)
                return userInfo.RedmineUserId;

            var redmineNameLower = userInfo.RedmineName?.ToLower();

            // don't want to think :) get all users from redmine and try to find id. it's executed very seldom
            string? strId = null;
            var xAllUsers = (await this.ExecuteRequest("users.xml")).Element("users");

            if (xAllUsers == null)
            {
                this._logger.Error("All users not found");
                return null;
            }

            foreach (var xUser in xAllUsers.Elements("user"))
            {
                var userLogin = xUser.Element("login")?.Value.ToLower();
                if (userLogin == redmineNameLower)
                {
                    strId = xUser.Element("id")?.Value;
                    if (strId == null)
                    {
                        this._logger.Error("Id not found in xml-user by login {xUser}", xUser);
                        return null;
                    }

                    break;
                }
                
                var userName = (xUser.Element("firstname")?.Value + " " + 
                                xUser.Element("lastname")?.Value).Trim().ToLower();
                if (userName == redmineNameLower)
                {
                    strId = xUser.Element("id")?.Value;
                    if (strId == null)
                    {
                        this._logger.Error("Id not found in xml-user by name 1 {xUser}", xUser);
                        return null;
                    }

                    break;
                }

                userName = (xUser.Element("lastname")?.Value + " " +
                            xUser.Element("firstname")?.Value).Trim().ToLower();
                if (userName == redmineNameLower)
                {
                    strId = xUser.Element("id")?.Value;
                    if (strId == null)
                    {
                        this._logger.Error("Id not found in xml-user by name 2 {xUser}", xUser);
                        return null;
                    }

                    break;
                }
            }

            if (strId == null)
            {
                this._logger.Error("User id not found");
                return null;
            }

            if (!int.TryParse(strId, out int id))
            {
                this._logger.Error("Error user id parse '{id}'", strId);
                return null;
            }

            userInfo = await this._dbContext.UsersInfo
                .FirstOrDefaultAsync(x => x.Id == userInfo.Id);

            userInfo.RedmineUserId = id;

            await this._dbContext.SaveChangesAsync();

            return id;

        }

        /// <summary> Get project id by project name </summary>
        /// <param name="projectSysName">Project name in bot system</param>
        /// <returns>Project id in redmine</returns>
        private async Task<int?> GetProjectId(string projectSysName)
        {
            var projectSettings = await this._dbContext.ProjectSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProjectSysName == projectSysName);
            if (projectSettings == null)
            {
                this._logger.Error("Find project info error by projectSysName = ({projectSysName})", projectSysName);
                return null;
            }

            if (projectSettings.RedmineProjectId != null)
                return projectSettings.RedmineProjectId;

            // don't want to think :) get all projects from redmine and try to find id. it's executed very seldom
            string? strId = null;

            var xAllProjects = (await this.ExecuteRequest("projects.xml")).Element("projects");
            if (xAllProjects == null)
            {
                this._logger.Error("All projects not found");
                return null;
            }

            foreach (var xProj in xAllProjects.Elements("project"))
            {
                var projectLowerName = xProj.Element("name")?.Value.ToLower();
                if (projectLowerName == projectSysName.ToLower())
                {
                    strId = xProj.Element("id")?.Value;
                    if (strId == null)
                    {
                        this._logger.Error("Id not found in xml-project {xProj}", xProj);
                        return null;
                    }

                    break;
                }
            }

            if (strId == null)
            {
                this._logger.Error("Project id not found");
                return null;
            }

            if (!int.TryParse(strId, out int id))
            {
                this._logger.Error("Error project id parse '{id}'", strId);
                return null;
            }

            projectSettings = await this._dbContext.ProjectSettings
                .FirstOrDefaultAsync(x => x.Id == projectSettings.Id);
            projectSettings.RedmineProjectId = id;

            await this._dbContext.SaveChangesAsync();
            
            return id;
        }

        /// <summary> Get version id by version name </summary>
        /// <param name="projectSysName">Project name in bot system</param>
        /// <param name="versionText">Text name of version</param>
        private async Task<int?> GetVersionId(string projectSysName, string versionText)
        {
            //maybe need caching
            this._logger.Warning("Very long getting version id");

            var projectSettings = await this._dbContext.ProjectSettings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProjectSysName == projectSysName);
            if (projectSettings?.RedmineProjectName == null)
            {
                this._logger.Error("Find project info error by projectSysName = ({projectSysName})", projectSysName);
                return null;
            }

            // don't want to think :) again (see upper)
            string? strId = null;
            var xVersions = (await this.ExecuteRequest($"/projects/{projectSettings.RedmineProjectName}/versions.xml")).Element("versions");


            if (xVersions == null)
            {
                this._logger.Error("Versions for project {project} not found", projectSettings.RedmineProjectName);
                return null;
            }

            foreach (var xVer in xVersions.Elements("version"))
            {
                var versionLowerName = xVer.Element("name")?.Value.ToLower();
                if (versionLowerName == versionText.ToLower())
                {
                    strId = xVer.Element("id")?.Value;
                    if (strId == null)
                    {
                        this._logger.Error("Id not found in xml-version {xVer}", xVer);
                        return null;
                    }

                    break;
                }
            }

            if (strId == null)
            {
                this._logger.Error("Version id not found");
                return null;
            }

            if (!int.TryParse(strId, out int id))
            {
                this._logger.Error("Error version id parse '{id}'", strId);
                return null;
            }

            // need caching value?
            return id;
        }


        //http://srm.aksiok.ru/issue_statuses.xml
        /// <summary> Get status id by name </summary>
        /// <param name="statusName">Status name</param>
        private async Task<int?> GetStatusId(string statusName)
        {
            //maybe need caching
            this._logger.Warning("Very long getting status id");

            // don't want to think :) again (see upper)
            string? strId = null;
            var xStatuses = (await this.ExecuteRequest("issue_statuses.xml")).Element("issue_statuses");


            if (xStatuses == null)
            {
                this._logger.Error("Statuses not found");
                return null;
            }

            foreach (var xStatus in xStatuses.Elements("issue_status"))
            {
                var statusLowerName = xStatus.Element("name")?.Value.ToLower();
                if (statusLowerName == statusName.ToLower())
                {
                    strId = xStatus.Element("id")?.Value;
                    if (strId == null)
                    {
                        this._logger.Error("Id not found in xml-status {xStatus}", xStatus);
                        return null;
                    }

                    break;
                }
            }

            if (strId == null)
            {
                this._logger.Error("Status id not found");
                return null;
            }

            if (!int.TryParse(strId, out int id))
            {
                this._logger.Error("Error status id parse '{id}'", strId);
                return null;
            }

            // need caching value?
            return id;
        }

    }
}