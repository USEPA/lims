using Microsoft.EntityFrameworkCore.Migrations;

namespace LimsServer.Migrations
{
    public partial class AddArchive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "archiveFolder",
                table: "Workflows",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "archiveFile",
                table: "Tasks",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archiveFolder",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "archiveFile",
                table: "Tasks");
        }
    }
}
