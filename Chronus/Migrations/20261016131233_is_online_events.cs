using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronus.Migrations
{
    /// <inheritdoc />
    public partial class is_online_events : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "CalendarEvent",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "CalendarEvent");
        }
    }
}
