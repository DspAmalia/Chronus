using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronus.Migrations
{
    /// <inheritdoc />
    public partial class meeting_attendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingAttendances_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeetingAttendances_CalendarEvent_EventId",
                        column: x => x.EventId,
                        principalTable: "CalendarEvent",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_EmployeeId",
                table: "MeetingAttendances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_EventId",
                table: "MeetingAttendances",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingAttendances");
        }
    }
}
