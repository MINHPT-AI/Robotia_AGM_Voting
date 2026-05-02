using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase07v2_PageMargins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "MarginBottom",
                table: "Templates",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "MarginLeft",
                table: "Templates",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "MarginRight",
                table: "Templates",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "MarginTop",
                table: "Templates",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarginBottom",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "MarginLeft",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "MarginRight",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "MarginTop",
                table: "Templates");
        }
    }
}
