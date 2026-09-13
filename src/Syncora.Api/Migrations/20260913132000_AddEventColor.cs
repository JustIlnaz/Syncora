using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Syncora.Migrations
{
    /// <inheritdoc />
    [Migration("20260913132000_AddEventColor")]
    public partial class AddEventColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
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
