using JenkinsService.Database;

namespace JenkinsService.Jenkins
{
    /// <summary> Information about job </summary>
    public class DtoJobChanged
    {
        /// <summary> Http address for opening information about build </summary>
        public string BuildUri { get; set; }

        /// <summary> Uri for build artifacts (binary files etc) </summary>
        public string ArtifactsUri { get; set; }

        /// <summary> Old build information </summary>
        public DbeJenkinsJob? OldBuildInfo { get; set; }
        
        /// <summary> New build information </summary>
        public DbeJenkinsJob? NewBuildInfo { get; set; }
    }
}