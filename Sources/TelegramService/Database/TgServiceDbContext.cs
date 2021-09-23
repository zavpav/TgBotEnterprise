using System;
using Microsoft.EntityFrameworkCore;

namespace TelegramService.Database
{
    public class TgServiceDbContext : DbContext
    {
        public TgServiceDbContext(DbContextOptions<TgServiceDbContext> options) : base(options)
        {
        }

        public DbSet<DtoUserInfo> UsersInfo { get; set; }
    }
}