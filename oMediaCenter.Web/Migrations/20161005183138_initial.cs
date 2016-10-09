using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace oMediaCenter.Web.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilePositions",
                columns: table => new
                {
                    FileHash = table.Column<string>(nullable: false),
                    LastPlayedPosition = table.Column<TimeSpan>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilePositions", x => x.FileHash);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilePositions");
        }
    }
}
