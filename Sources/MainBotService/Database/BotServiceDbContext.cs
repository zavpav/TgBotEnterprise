using Microsoft.EntityFrameworkCore;

namespace MainBotService.Database
{
    public class BotServiceDbContext : DbContext
    {
        public BotServiceDbContext(DbContextOptions<BotServiceDbContext> options) : base(options)
        {
        }

#nullable disable
        
        /// <summary> List of users </summary>
        public DbSet<DbeUserInfo> UsersInfo { get; set; }

        /// <summary> List of projects </summary>
//        public DbSet<DbeProject> Projects { get; set; }

#nullable restore
    }
}