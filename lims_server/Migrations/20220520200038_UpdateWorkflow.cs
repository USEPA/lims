using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimsServer.Migrations
{
    public partial class UpdateWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "filter",
                table: "Workflows",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "multiFile",
                table: "Workflows",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "filter",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "multiFile",
                table: "Workflows");
        }
    }
}
