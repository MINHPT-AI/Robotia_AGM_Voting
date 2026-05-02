using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentPosition",
                table: "meeting_candidates",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "companies",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SealImagePath",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureImagePath",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockExchange",
                table: "companies",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPosition",
                table: "meeting_candidates");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "SealImagePath",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "SignatureImagePath",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "StockExchange",
                table: "companies");
        }
    }
}
