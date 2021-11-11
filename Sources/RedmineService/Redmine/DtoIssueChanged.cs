using RedmineService.Database;

namespace RedmineService.Redmine
{
    /// <summary> Tuple of changed issue </summary>
    public class DtoIssueChanged
    {
        /// <summary> Old version of issue </summary>
        public DbeIssue? OldVersion { get; set; }
        
        /// <summary> New version of issue </summary>
        public DbeIssue? NewVersion { get; set; }
        
        /// <summary> Has new comments </summary>
        public bool HasComment { get; set; }
        
        /// <summary> Has any changes </summary>
        public bool HasChanges { get; set; }
        
        /// <summary> Last changed users </summary>
        public string[]? RedmineUserLastChanged { get; set; }
    }
}