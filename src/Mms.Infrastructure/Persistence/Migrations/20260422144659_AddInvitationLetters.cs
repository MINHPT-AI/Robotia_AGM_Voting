using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationLetters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invitation_letters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareholderIdNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ShareholderName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ShareholderAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShareholderPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VotingRights = table.Column<long>(type: "bigint", nullable: false),
                    SharesTotal = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TrackingCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DispatchedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StatusUpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CodeMarkType = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitation_letters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invitation_letters_meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invitation_letters_MeetingId",
                table: "invitation_letters",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_invitation_letters_MeetingId_Status",
                table: "invitation_letters",
                columns: new[] { "MeetingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_invitation_letters_TrackingCode",
                table: "invitation_letters",
                column: "TrackingCode",
                filter: "\"TrackingCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invitation_letters");
        }
    }
}
