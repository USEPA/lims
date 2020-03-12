using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LimsServer.Migrations
{
    public partial class Migration_031220 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Processors",
                columns: table => new
                {
                    id = table.Column<string>(nullable: false),
                    name = table.Column<string>(nullable: true),
                    version = table.Column<string>(nullable: true),
                    description = table.Column<string>(nullable: true),
                    file_type = table.Column<string>(nullable: true),
                    enabled = table.Column<bool>(nullable: false),
                    process_found = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    id = table.Column<string>(nullable: false),
                    taskID = table.Column<string>(nullable: true),
                    workflowID = table.Column<string>(nullable: true),
                    inputFile = table.Column<string>(nullable: true),
                    outputFile = table.Column<string>(nullable: true),
                    status = table.Column<string>(nullable: true),
                    message = table.Column<string>(nullable: true),
                    start = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Username = table.Column<string>(nullable: true),
                    Enabled = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<byte[]>(nullable: true),
                    PasswordSalt = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    id = table.Column<string>(nullable: false),
                    name = table.Column<string>(nullable: true),
                    processor = table.Column<string>(nullable: true),
                    inputFolder = table.Column<string>(nullable: true),
                    outputFolder = table.Column<string>(nullable: true),
                    interval = table.Column<int>(nullable: false),
                    message = table.Column<string>(nullable: true),
                    active = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Processors");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Workflows");
        }
    }
}
