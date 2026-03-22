using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace oMediaCenter.Web.Migrations
{
    /// <inheritdoc />
    public partial class CachedMediaInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedMediaInformationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Filename = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    OtherInfo = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<string>(type: "text", nullable: true),
                    ImdbNumber = table.Column<string>(type: "text", nullable: true),
                    VideoType = table.Column<string>(type: "text", nullable: true),
                    Episode = table.Column<string>(type: "text", nullable: true),
                    Season = table.Column<string>(type: "text", nullable: true),
                    Genres = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedMediaInformationRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedMediaInformationRecords_Filename",
                table: "CachedMediaInformationRecords",
                column: "Filename",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedMediaInformationRecords");
        }
    }
}
