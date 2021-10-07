using Microsoft.EntityFrameworkCore;

namespace JenkinsService.Database
{
    public class JenkinsDbContext : DbContext
    {
        public JenkinsDbContext(DbContextOptions<JenkinsDbContext> options) : base(options)
        {
        }

#nullable disable
        public DbSet<DtoUserInfo> UsersInfo { get; set; }
#nullable restore
    }
}