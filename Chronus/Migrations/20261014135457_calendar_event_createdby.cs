using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronus.Migrations
{
    /// <inheritdoc />
    public partial class calendar_event_createdby : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "CalendarEvent",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "CalendarEvent");
        }
    }
}
