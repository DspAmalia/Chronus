using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronus.Migrations
{
    /// <inheritdoc />
    public partial class calendar_event_participants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Participants",
                table: "CalendarEvent",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Participants",
                table: "CalendarEvent");
        }
    }
}
