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
            // calendar_members never had Color — do not drop it (that broke MigrateAsync).
            // Color may already exist from AddEventColor / EnsureSchema; add only if missing via raw SQL.
            migrationBuilder.Sql(
                """ALTER TABLE meetings ADD COLUMN IF NOT EXISTS "Description" text;""");

            migrationBuilder.Sql(
                """ALTER TABLE events ADD COLUMN IF NOT EXISTS "Color" character varying(20);""");

            migrationBuilder.Sql(
                """ALTER TABLE events ADD COLUMN IF NOT EXISTS "MeetingId" uuid;""");

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_events_MeetingId" ON events ("MeetingId");
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'FK_events_meetings_MeetingId'
                    ) THEN
                        ALTER TABLE events
                            ADD CONSTRAINT "FK_events_meetings_MeetingId"
                            FOREIGN KEY ("MeetingId") REFERENCES meetings ("Id")
                            ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
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
        }
    }
}
