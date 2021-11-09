using RedmineService.Database;

namespace RedmineService.Redmine
{
    /// <summary> Tuple of changed issue </summary>
    public class DtoIssueChanged
    {
        public DbeIssue? OldVersion { get; set; }
        public DbeIssue? NewVersion { get; set; }
    }
}