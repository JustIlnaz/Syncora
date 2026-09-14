using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Syncora.Data;

#nullable disable

namespace Syncora.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SyncoraDbContext))]
    [Migration("20260913132000_AddEventColor")]
    public partial class AddEventColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: column may already exist from EnsureSchema / later migration.
            migrationBuilder.Sql(
                """ALTER TABLE events ADD COLUMN IF NOT EXISTS "Color" character varying(20);""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "events");
        }
    }
}
