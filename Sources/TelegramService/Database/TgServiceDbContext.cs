using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace TelegramService.Database
{
    public class TgServiceDbContext : DbContext
    {
        public TgServiceDbContext(DbContextOptions<TgServiceDbContext> options) : base(options)
        {
        }

#nullable disable
        public DbSet<DbeUserInfo> UsersInfo { get; set; }
#nullable restore
    }
}