using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LimsServer.Migrations
{
    public partial class AddLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    id = table.Column<string>(nullable: false),
                    workflowId = table.Column<string>(nullable: true),
                    taskId = table.Column<string>(nullable: true),
                    taskHangfireID = table.Column<string>(nullable: true),
                    processorId = table.Column<string>(nullable: true),
                    message = table.Column<string>(nullable: true),
                    type = table.Column<string>(nullable: true),
                    time = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Logs");
        }
    }
}
