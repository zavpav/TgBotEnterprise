using Microsoft.EntityFrameworkCore.Migrations;

namespace RedmineService.Database.Migrations
{
    public partial class MyTaskProjessing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RedmineUserId",
                table: "UsersInfo",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RedmineProjectId",
                table: "ProjectSettings",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RedmineUserId",
                table: "UsersInfo");

            migrationBuilder.DropColumn(
                name: "RedmineProjectId",
                table: "ProjectSettings");
        }
    }
}
