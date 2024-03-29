﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RedmineService.Database;

namespace RedmineService.Database.Migrations
{
    [DbContext(typeof(RedmineDbContext))]
    [Migration("20211109154658_Issues")]
    partial class Issues
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.10")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("RedmineService.Database.DbeIssue", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("CreateOn")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("CreatorName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("IssueStatus")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Num")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProjectSysName")
                        .HasColumnType("text");

                    b.Property<string>("RedmineAssignOn")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("RedminePriority")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("RedmineProjectName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("RedmineStatus")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Resolution")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdateOn")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("UserBotIdAssignOn")
                        .HasColumnType("text");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Issues");
                });

            modelBuilder.Entity("RedmineService.Database.DbeProjectSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("ProjectSysName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("RedmineProjectId")
                        .HasColumnType("integer");

                    b.Property<string>("RedmineProjectName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("VersionMask")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ProjectSettings");
                });

            modelBuilder.Entity("RedmineService.Database.DbeUserInfo", b =>
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

                    b.Property<string>("RedmineName")
                        .HasColumnType("text");

                    b.Property<int?>("RedmineUserId")
                        .HasColumnType("integer");

                    b.Property<string>("WhoIsThis")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("UsersInfo");
                });
#pragma warning restore 612, 618
        }
    }
}
