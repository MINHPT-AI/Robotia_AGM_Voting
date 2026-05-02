using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase07v2_TemplateEditorSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HtmlContent",
                table: "Templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedTokens",
                table: "Templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseSignatureAndSeal",
                table: "Templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HtmlContent",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SelectedTokens",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "UseSignatureAndSeal",
                table: "Templates");
        }
    }
}
