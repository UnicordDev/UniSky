using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniSky.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class PlatformVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlatformVersion",
                table: "Registrations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlatformVersion",
                table: "Registrations");
        }
    }
}
