using Microsoft.EntityFrameworkCore.Migrations;

namespace oMediaCenter.MetaDatabase.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaDatum",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(nullable: false),
					LowercaseTitle = table.Column<string>(nullable: false),
                    OriginalString = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaDatum", x => x.Id);
                });

			migrationBuilder.CreateIndex("IX_MediaDatum_Title", "MediaDatum", "LowercaseTitle");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropIndex("IX_MediaDatum_Title", "MediaDatum");
            migrationBuilder.DropTable(
                name: "MediaDatum");
        }
    }
}
