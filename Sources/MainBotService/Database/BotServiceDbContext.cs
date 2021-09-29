using Microsoft.EntityFrameworkCore;

namespace MainBotService.Database
{
    public class BotServiceDbContext : DbContext
    {
        public BotServiceDbContext(DbContextOptions<BotServiceDbContext> options) : base(options)
        {
        }

#nullable disable
        public DbSet<DtoUserInfo> UsersInfo { get; set; }
#nullable restore
    }
}