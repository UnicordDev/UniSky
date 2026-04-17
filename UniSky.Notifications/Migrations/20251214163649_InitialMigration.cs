using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniSky.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Did = table.Column<string>(type: "TEXT", nullable: false),
                    InstallId = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Options = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => new { x.Did, x.InstallId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ChannelUrl",
                table: "Registrations",
                column: "ChannelUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_Did",
                table: "Registrations",
                column: "Did");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Registrations");
        }
    }
}
