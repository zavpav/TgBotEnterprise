using Microsoft.EntityFrameworkCore;

namespace RedmineService.Database
{
    public class RedmineDbContext : DbContext
    {
        public RedmineDbContext(DbContextOptions<RedmineDbContext> options) : base(options)
        {
        }

// ReSharper disable UnusedAutoPropertyAccessor.Global
#nullable disable

        /// <summary> Infomration about bot users in redmine server </summary>
        public DbSet<DbeUserInfo> UsersInfo { get; set; }

        /// <summary> Information about project settings in redmine </summary>
        public DbSet<DbeProjectSettings> ProjectSettings { get; set; }

#nullable restore
// ReSharper restore UnusedAutoPropertyAccessor.Global
    }
}