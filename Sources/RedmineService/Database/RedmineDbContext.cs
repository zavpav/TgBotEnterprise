using Microsoft.EntityFrameworkCore;

namespace RedmineService.Database
{
    public class RedmineDbContext : DbContext
    {
        public RedmineDbContext(DbContextOptions<RedmineDbContext> options) : base(options)
        {
        }

#nullable disable
        public DbSet<DtoUserInfo> UsersInfo { get; set; }
#nullable restore
    }
}