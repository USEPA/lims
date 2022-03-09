using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsServer.Migrations
{
    public partial class LogAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "action",
                table: "Logs",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "action",
                table: "Logs");
        }
    }
}
