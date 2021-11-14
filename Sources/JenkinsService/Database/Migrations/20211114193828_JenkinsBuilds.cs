using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace JenkinsService.Database.Migrations
{
    public partial class JenkinsBuilds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JenkinsJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JenkinsProjectName = table.Column<string>(type: "text", nullable: false),
                    JenkinsBuildStarter = table.Column<string>(type: "text", nullable: false),
                    BuildNumber = table.Column<string>(type: "text", nullable: false),
                    BuildName = table.Column<string>(type: "text", nullable: false),
                    BuildDescription = table.Column<string>(type: "text", nullable: false),
                    BuildStatus = table.Column<string>(type: "text", nullable: false),
                    BuildIsProcessing = table.Column<bool>(type: "boolean", nullable: false),
                    BuildDuration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BuildBranchName = table.Column<string>(type: "text", nullable: false),
                    ProjectSysName = table.Column<string>(type: "text", nullable: true),
                    BuildTupe = table.Column<string>(type: "text", nullable: false),
                    UserBotIdAssignOn = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JenkinsJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JenkinsJobId = table.Column<int>(type: "integer", nullable: false),
                    GitComment = table.Column<string>(type: "text", nullable: false),
                    ProjectName = table.Column<string>(type: "text", nullable: true),
                    IssueId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeInfo_JenkinsJobs_JenkinsJobId",
                        column: x => x.JenkinsJobId,
                        principalTable: "JenkinsJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeInfo_JenkinsJobId",
                table: "ChangeInfo",
                column: "JenkinsJobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeInfo");

            migrationBuilder.DropTable(
                name: "JenkinsJobs");
        }
    }
}
