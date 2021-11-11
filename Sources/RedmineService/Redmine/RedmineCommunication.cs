using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommonInfrastructure;
using Microsoft.EntityFrameworkCore;
using RabbitMessageCommunication.BugTracker;
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


        /// <summary> Get full prefix for "issue" </summary>
        /// <returns></returns>
        /// <returns>Http part. If we concat this part with "issue num" we will receive full address for opening issue</returns>
        public async ValueTask<string> GetHttpPrefixOfIssue()
        {
            return (await this.GetRedmineHost()) + "/issues/";
        }

        /// <summary> Execute query. Add main url part. Configure credintals and etc </summary>
        private async Task<XDocument> ExecuteRequest(string requestPart)
        {
            var redmineHost = await GetRedmineHost();

            var uri = redmineHost + requestPart;
            return await HttpExtension.LoadXmlFromRequest(uri, 
                TimeSpan.FromSeconds(9), 
                this.AddCredential,
                this.UpdateCredential,
                this._logger);
        }

        /// <summary> Cache of redmine host </summary>
        private string? _redmineHost = null;
        
        /// <summary> Get redmine host </summary>
        private async ValueTask<string> GetRedmineHost()
        {
            if (this._redmineHost != null)
                return this._redmineHost;

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

            this._redmineHost = redmineHost ?? throw new NotSupportedException("Redmine host undefined");
            return this._redmineHost;
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
        public async Task<List<DbeIssue>> SimpleFindIssues(string? userBotId,
            string? projectSysName,
            string? versionText)
        {
            await Task.Yield();

            var issuesQuery = this._dbContext.Issues.AsQueryable();
            if (userBotId != null)
                issuesQuery = issuesQuery.Where(x => x.UserBotIdAssignOn == userBotId);
            if (projectSysName != null)
                issuesQuery = issuesQuery.Where(x => x.ProjectSysName == projectSysName);
            if (versionText != null)
                issuesQuery = issuesQuery.Where(x => x.Version == versionText);

            return await issuesQuery.ToListAsync();
        }

        /// <summary> Update database with issues </summary>
        public async Task<List<DtoIssueChanged>> UpdateIssuesDb()
        {
            var issuesChanged = new List<DtoIssueChanged>();

            var updatedIssues = await this.GetUpdatedIssues();
            foreach (var issue in updatedIssues)
            {
                var savedIssue = await this._dbContext.Issues
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Num == issue.Num);

                // Workaround for a little error in Redmine
                // Skip "last issue"
                if (savedIssue?.UpdateOn == issue.UpdateOn)
                    continue;

                // skip untracking issues (doesn't have botId in any ("old" or "new" version) information about user
                if (!string.IsNullOrEmpty(issue.UserBotIdAssignOn) 
                    || !string.IsNullOrEmpty(savedIssue?.UserBotIdAssignOn))
                {
                    var issueChanged = new DtoIssueChanged
                    {
                        OldVersion = savedIssue,
                        NewVersion = issue
                    };
                    if (savedIssue != null)
                    {
                        var journals = await this.GetIssueJournals(savedIssue.Num);
                        var newJournals = journals.Where(x => x.CreateOn > savedIssue.UpdateOn).ToList();
                        if (newJournals.Any(x => x.HasComment))
                            issueChanged.HasComment = true;
                        if (newJournals.Any(x => x.HasDetails))
                            issueChanged.HasChanges = true;
                        issueChanged.RedmineUserLastChanged = newJournals.Select(x => x.RedmineUser).Distinct().ToArray();
                    }

                    issuesChanged.Add(issueChanged);
                }

                try
                {
                    if (savedIssue == null)
                        this._dbContext.Issues.Add(issue); // add new issue
                    else
                    {
                        savedIssue = await this._dbContext.Issues
                            .SingleOrDefaultAsync(x => x.Num == issue.Num); // find again with tracking info
                        issue.Id = savedIssue.Id;
                        this._dbContext.Entry(savedIssue).CurrentValues.SetValues(issue);// update information in dbContext
                    }
                }
                catch (Exception e)
                {
                    this._logger
                        .ForContext("issue", issue, destructureObjects: true)
                        .Error(e, "Error add/modify issue while start redmine {num}", issue.Num);
                    throw;
                }
            }

            try
            {
                if (this._dbContext.ChangeTracker.HasChanges())
                    await this._dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                this._logger.Error(e, "Error update issues while start redmine");
                throw;
            }
            return issuesChanged;
        }

        /// <summary> Information from issue journal. Don't want to save </summary>
        private class RedmineJournal
        {
#nullable disable
            public string JournalId { get; set; }
            public string RedmineUser { get; set; }
#nullable enable
            public bool HasComment { get; set; }
            public bool HasDetails { get; set; }
            public DateTime CreateOn { get; set; }
        }

        /// <summary> Get journal information from issue </summary>
        /// <param name="num">Issue num</param>
        private async Task<List<RedmineJournal>> GetIssueJournals(string num)
        {
            var xFullIssue = await this.ExecuteRequest($"issues/{num}.xml?include=journals");
            var xJournals = xFullIssue?.Element("issue")?.Element("journals");
            if (xJournals == null)
            {
                this._logger
                    .ForContext("fullIssue", xFullIssue, true)
                    .Error("Error issue #{num}", num); 
                return new List<RedmineJournal>();
            }

            var journals = new List<RedmineJournal>();

            foreach (var xJournal in xJournals.Elements("journal"))
            {
                var redmineJournal = new RedmineJournal
                {
                    JournalId = xJournal.Attribute("id")?.Value ?? "",
                    RedmineUser = xJournal.Element("user")?.Attribute("name")?.Value ?? "",
                    HasComment = !string.IsNullOrEmpty(xJournal.Element("notes")?.Value),
                    HasDetails = xJournal.Element("details")?.Elements("detail").Count() != 0,
                    CreateOn = this.GetDateTimeFromXml(xJournal, "created_on")
                };
                journals.Add(redmineJournal);
            }

            return journals;
        }



        /// <summary> Get last updated issues </summary>
        /// <remarks>Has a little error and always return last issue (read comments iGetIssuesByDateDirect)</remarks>
        private async Task<List<DbeIssue>> GetUpdatedIssues()
        {
            var lastUpdateOn = await this._dbContext.Issues.AsNoTracking().MaxAsync(x => (DateTime?)x.UpdateOn);
            var allChangedIssues = new List<DbeIssue>();
            if (lastUpdateOn == null)
            {
                var veryOldDate = new DateTime(1800, 1, 1, 0, 0, 0);
                var lastIssue = await this.GetIssuesByDateDirect(
                    veryOldDate,
                    isSingle: true);
                if (lastIssue.Count != 0)
                {
                    var toDate = lastIssue.Single().UpdateOn;
                    // Load some iteration because I don't need more
                    for (var iter = 0; iter < 4; iter++)
                    {
                        var issues = await this.GetIssuesByDateDirect(veryOldDate, toDate);
                        // remove duplicates (maybe with the same date)
                        issues.RemoveAll(x => allChangedIssues.Any(xx => xx.Num == x.Num));
                        allChangedIssues.AddRange(issues);
                        toDate = issues.Min(x => x.UpdateOn);
                    }
                }
            }
            else
            {
                // maybe need to add 1 second that don't get last issue, but redmine has a little error (read comments in GetIssuesByDateDirect)
                allChangedIssues.AddRange(await this.GetIssuesByDateDirect(lastUpdateOn.Value));
            }

            // Restore bot fields
            var projects = await this._dbContext.ProjectSettings.ToListAsync();
            var users = await this._dbContext.UsersInfo.Where(x => !string.IsNullOrEmpty(x.RedmineName)).ToListAsync();
            foreach (var issue in allChangedIssues)
            {
                //IssueStatus
                issue.IssueStatus = issue.RedmineStatus.ToLower() switch
                {
                    "черновик" => EnumIssueStatus.Draft,
                    "анализируется" => EnumIssueStatus.Draft,
                    "готов к работе" => EnumIssueStatus.Ready,
                    "переоткрыт" => EnumIssueStatus.Ready,
                    "в работе" => EnumIssueStatus.Developing,
                    "решен" => EnumIssueStatus.Resolved,
                    "на тестировании" => EnumIssueStatus.OnTesting,
                    "проверен" => EnumIssueStatus.Finished,
                    "на документировании" => EnumIssueStatus.Finished,
                    "закрыт" => EnumIssueStatus.Finished,
                    var strStatus => ((Func<EnumIssueStatus>)(() => {
                        this._logger.Warning("Undefined redmine status {redmineStatus}", strStatus);
                        return EnumIssueStatus.NotDefined;
                    }))()
                };

                //ProjectSysName
                var projectSttings = projects
                    .Where(x => x.RedmineProjectName.ToLower() == issue.RedmineProjectName.ToLower())
                    .ToList();
                if (projectSttings.Count == 1)
                    issue.ProjectSysName = projectSttings.Single().ProjectSysName;
                else if (projectSttings.Count > 1)
                {
                    this._logger.Error("Error find project name (maybe some sysProjects contains in one RemineProject, need check version). Not realized yet. {remineProject}=>{@foundSettings}",
                        issue.RedmineProjectName,
                        projectSttings.Select(x => x.ProjectSysName).ToArray());
                }

                //UserBotIdAssignOn
                var assignUsers = users.Where(x => x.RedmineName != null && x.RedmineName.ToLower() == issue.RedmineAssignOn.ToLower()).ToList();
                if (assignUsers.Count == 1)
                    issue.UserBotIdAssignOn = assignUsers.Single().BotUserId;
                else if (assignUsers.Count > 1)
                {
                    this._logger.Error("Error find user. {redmineUser}=>{@foundUsers}",
                        issue.RedmineAssignOn,
                        assignUsers.Select(x => x.BotUserId).ToArray());
                }
            }

            return allChangedIssues;
        }

        /// <summary> Get issues from Redmine by date. It's not fill "other information" like UserBotId and so on. </summary>
        /// <remarks>Has a little error and "always" return last issue</remarks>
        private async Task<List<DbeIssue>> GetIssuesByDateDirect(DateTime dateFrom, DateTime? dateTo = null, bool isSingle = false)
        {
            // updated_on=%3E%3D2020-01-01  updated_on= >=2020-01-01
            // updated_on=%3C%3D <=

            // this approach has a little error. Redmine returns issue updated earlier than I query.
            // for example
            // Last issue updated 2021-01-01T10:00:00
            // if I request updateon=>=2021-01-01T10:00:01 Remine return last issue
            // if I request updateon=>=2021-01-01T10:00:02 Remine doesn't return last issue
            // strange. But I filter by date in "caller"

            var issuesRequest = "issues.xml?sort=updated_on:desc&limit=" + (isSingle ? "1" : "100")
                                + "&updated_on=%3E%3D" + dateFrom.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
            if (dateTo != null)
                issuesRequest += "&updated_on=%3C%3D" + dateTo.Value.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";

            var xIssues = (await this.ExecuteRequest(issuesRequest)).Element("issues");
            var issues = new List<DbeIssue>();
            // Find Redmine information
            if (xIssues != null)
            {
                foreach (var xIssue in xIssues.Elements("issue"))
                {
                    try
                    {
                        issues.Add(this.CreateStubIssueByXmlElement(xIssue));
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
                return new List<DbeIssue>();
            }

            return issues;
        }


        /// <summary> CreateDbeIssue without information about user, bot project and so on </summary>
        /// <param name="xIssue">Xml issue information</param>
        /// <returns></returns>
        private DbeIssue CreateStubIssueByXmlElement(XElement xIssue)
        {
            var issue = new DbeIssue
            {
                Num = xIssue.Element("id")?.Value ?? "<not defined>",
                Subject = xIssue.Element("subject")?.Value ?? "",
                Description = xIssue.Element("description")?.Value ?? "",
                RedmineStatus = xIssue.Element("status")?.Attribute("name")?.Value ?? "",
                RedmineProjectName = xIssue.Element("project")?.Attribute("name")?.Value ?? "",
                Version = xIssue.Element("fixed_version")?.Attribute("name")?.Value ?? "",
                CreatorName = xIssue.Element("author")?.Attribute("name")?.Value ?? "",
                RedmineAssignOn = xIssue.Element("assigned_to")?.Attribute("name")?.Value ?? "",
                RedminePriority = xIssue.Element("priority")?.Attribute("name")?.Value ?? ""
            };

            var resolution = xIssue.Element("custom_fields")
                ?.Elements("custom_field")
                .FirstOrDefault(x => x.Attribute("name")?.Value == "Резолюция")
                ?.Value;
            issue.Resolution = resolution ?? "";

            issue.UpdateOn = this.GetDateTimeFromXml(xIssue, "updated_on");
            issue.CreateOn = this.GetDateTimeFromXml(xIssue, "created_on");

            return issue;
        }

        /// <summary> Get date from redmine xml </summary>
        /// <param name="xE">xml element</param>
        /// <param name="field">Date element</param>
        /// <returns>DateTime or null if field doesn't exist</returns>
        private DateTime GetDateTimeFromXml(XElement xE, string field)
        {
            var dateStr = xE.Element(field)?.Value;
            if (dateStr != null)
            {
                //2020-03-18T12:48:35Z
                var dt = DateTime.ParseExact(dateStr, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                return dt;
            }

            return default;
        }

        #region Old functions. It's a pity to delete.

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

            if (projectSettings.RedmineProjectName == null)
            {
                this._logger.Error("Undefined project for find {projectSysName}", projectSysName);
                return null;
            }

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
                if (projectLowerName == projectSettings.RedmineProjectName.ToLower())
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

        /// <summary> Get status id by name </summary>
        /// <param name="statusName">Status name</param>
        private async Task<int?> GetStatusId(string statusName)
        {
            //http://srm.aksiok.ru/issue_statuses.xml

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
        #endregion
    }
}