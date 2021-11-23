using Microsoft.EntityFrameworkCore.Migrations;

namespace JenkinsService.Database.Migrations
{
    public partial class GitProjectPrefixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitProjectPrefixes",
                table: "ProjectSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProjectSysName",
                table: "ChangeInfo",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitProjectPrefixes",
                table: "ProjectSettings");

            migrationBuilder.DropColumn(
                name: "ProjectSysName",
                table: "ChangeInfo");
        }
    }
}
