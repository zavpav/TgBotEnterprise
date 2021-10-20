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
        public DbSet<DtoUserInfo> UsersInfo { get; set; }

        /// <summary> Information about project settings in redmine </summary>
        public DbSet<DtoProjectSettings> ProjectSettings { get; set; }

#nullable restore
// ReSharper restore UnusedAutoPropertyAccessor.Global
    }
}