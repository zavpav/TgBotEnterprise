using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        /// <summary> All issues from redmine </summary>
        public DbSet<DbeIssue> Issues { get; set; }

#nullable restore
        // ReSharper restore UnusedAutoPropertyAccessor.Global

        protected override void OnModelCreating(ModelBuilder model)
        {
            foreach (var entityType in model.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType.BaseType == typeof(Enum))
                    {
                        var type = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                        var converter = Activator.CreateInstance(type, new ConverterMappingHints()) as ValueConverter;

                        property.SetValueConverter(converter);
                    }
                }
            }
        }

    }
}