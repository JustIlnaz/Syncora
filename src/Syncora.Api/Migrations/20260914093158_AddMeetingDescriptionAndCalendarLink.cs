using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syncora.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingDescriptionAndCalendarLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "calendar_members");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "meetings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MeetingId",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_events_MeetingId",
                table: "events",
                column: "MeetingId");

            migrationBuilder.AddForeignKey(
                name: "FK_events_meetings_MeetingId",
                table: "events",
                column: "MeetingId",
                principalTable: "meetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_events_meetings_MeetingId",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_events_MeetingId",
                table: "events");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "meetings");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "events");

            migrationBuilder.DropColumn(
                name: "MeetingId",
                table: "events");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "calendar_members",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
