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
        public DbSet<DtoUserInfo> UsersInfo { get; set; }

        /// <summary> List of projects </summary>
//        public DbSet<DtoProject> Projects { get; set; }

#nullable restore
    }
}