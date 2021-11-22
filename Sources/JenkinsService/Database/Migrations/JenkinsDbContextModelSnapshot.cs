﻿// <auto-generated />
using System;
using JenkinsService.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace JenkinsService.Database.Migrations
{
    [DbContext(typeof(JenkinsDbContext))]
    partial class JenkinsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.10")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("JenkinsService.Database.DbeJenkinsJob", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("BuildBranchName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("BuildDescription")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<TimeSpan>("BuildDuration")
                        .HasColumnType("interval");

                    b.Property<bool>("BuildIsProcessing")
                        .HasColumnType("boolean");

                    b.Property<string>("BuildName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("BuildNumber")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("BuildStatus")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("BuildSubType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("JenkinsBuildStarter")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("JenkinsBuildStatus")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("JenkinsJobName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProjectSysName")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("JenkinsJobs");
                });

            modelBuilder.Entity("JenkinsService.Database.DbeJenkinsJob+ChangeInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("GitComment")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("IssueId")
                        .HasColumnType("text");

                    b.Property<int>("JenkinsJobId")
                        .HasColumnType("integer");

                    b.Property<string>("ProjectName")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("JenkinsJobId");

                    b.ToTable("ChangeInfo");
                });

            modelBuilder.Entity("JenkinsService.Database.DbeProjectSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("ProjectSysName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ProjectSettings");
                });

            modelBuilder.Entity("JenkinsService.Database.DbeProjectSettings+JobDescription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("JobPath")
                        .HasColumnType("text");

                    b.Property<string>("JobType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ParentId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("ProjectSettingsJobDescription");
                });

            modelBuilder.Entity("JenkinsService.Database.DbeUserInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("BotUserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<string>("JenkinsName")
                        .HasColumnType("text");

                    b.Property<string>("WhoIsThis")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("UsersInfo");
                });

            modelBuilder.Entity("JenkinsService.Database.DbeJenkinsJob+ChangeInfo", b =>
                {
                    b.HasOne("JenkinsService.Database.DbeJenkinsJob", "JenkinsJob")
                        .WithMany("ChangeInfos")
                        .HasForeignKey("JenkinsJobId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("JenkinsJob");
                });

            modelBuilder.Entity("JenkinsService.Database.DbeProjectSettings+JobDescription", b =>
                {
                    b.HasOne("JenkinsService.Database.DbeProjectSettings", "Parent")
                        .WithMany("JobInformations")
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("JenkinsService.Database.DbeJenkinsJob", b =>
                {
                    b.Navigation("ChangeInfos");
                });

            modelBuilder.Entity("JenkinsService.Database.DbeProjectSettings", b =>
                {
                    b.Navigation("JobInformations");
                });
#pragma warning restore 612, 618
        }
    }
}
