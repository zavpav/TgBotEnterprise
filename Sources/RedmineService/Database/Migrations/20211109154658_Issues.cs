using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace RedmineService.Database.Migrations
{
    public partial class Issues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Num = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Resolution = table.Column<string>(type: "text", nullable: false),
                    RedmineStatus = table.Column<string>(type: "text", nullable: false),
                    IssueStatus = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    RedminePriority = table.Column<string>(type: "text", nullable: false),
                    RedmineProjectName = table.Column<string>(type: "text", nullable: false),
                    ProjectSysName = table.Column<string>(type: "text", nullable: true),
                    CreateOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdateOn = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UserBotIdAssignOn = table.Column<string>(type: "text", nullable: true),
                    RedmineAssignOn = table.Column<string>(type: "text", nullable: false),
                    CreatorName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Issues");
        }
    }
}
