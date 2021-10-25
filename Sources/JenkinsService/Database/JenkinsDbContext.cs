using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JenkinsService.Database
{
    public class JenkinsDbContext : DbContext
    {
        public JenkinsDbContext(DbContextOptions<JenkinsDbContext> options) : base(options)
        {
        }

// ReSharper disable UnusedAutoPropertyAccessor.Global
#nullable disable

        /// <summary> Infomration about bot users in jenkins server </summary>
        public DbSet<DbeUserInfo> UsersInfo { get; set; }
        
        /// <summary> Information about project settings in jenkins </summary>
        public DbSet<DbeProjectSettings> ProjectSettings { get; set; }

        /// <summary> Information about job settings of projects </summary>
        public DbSet<DbeProjectSettings.JobDescription> ProjectSettingsJobDescription { get; set; }

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