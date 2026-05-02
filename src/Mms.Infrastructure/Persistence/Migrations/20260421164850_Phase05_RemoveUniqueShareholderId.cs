using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase05_RemoveUniqueShareholderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shareholders_MeetingId_IdNumber",
                table: "shareholders");

            migrationBuilder.CreateIndex(
                name: "IX_shareholders_MeetingId_IdNumber",
                table: "shareholders",
                columns: new[] { "MeetingId", "IdNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shareholders_MeetingId_IdNumber",
                table: "shareholders");

            migrationBuilder.CreateIndex(
                name: "IX_shareholders_MeetingId_IdNumber",
                table: "shareholders",
                columns: new[] { "MeetingId", "IdNumber" },
                unique: true);
        }
    }
}
