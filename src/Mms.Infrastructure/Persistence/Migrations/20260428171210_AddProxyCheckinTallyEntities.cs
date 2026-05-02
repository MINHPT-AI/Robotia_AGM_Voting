using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProxyCheckinTallyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proxies_meetings_MeetingId",
                table: "Proxies");

            migrationBuilder.DropForeignKey(
                name: "FK_Proxies_shareholders_GrantorId",
                table: "Proxies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Proxies",
                table: "Proxies");

            migrationBuilder.DropIndex(
                name: "IX_Proxies_MeetingId",
                table: "Proxies");

            migrationBuilder.RenameTable(
                name: "Proxies",
                newName: "proxies");

            migrationBuilder.RenameIndex(
                name: "IX_Proxies_GrantorId",
                table: "proxies",
                newName: "IX_proxies_GrantorId");

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                table: "proxies",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "ProxyType",
                table: "proxies",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "GranteeName",
                table: "proxies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "GranteeIdNumber",
                table: "proxies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "proxies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GranteeRecipientId",
                table: "proxies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GranteeShareholderId",
                table: "proxies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "proxies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededById",
                table: "proxies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPrintMode",
                table: "meetings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GiftEnabled",
                table: "meetings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "QuorumThreshold",
                table: "meetings",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovalThreshold",
                table: "meeting_resolutions",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionType",
                table: "meeting_resolutions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CandidateBoard",
                table: "meeting_candidates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfSeats",
                table: "meeting_candidates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AttendanceRecordId",
                table: "ballots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BallotType",
                table: "ballots",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "BulkApproved",
                table: "ballots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSplitBallot",
                table: "ballots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProxyRepresentationNote",
                table: "ballots",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SplitSequence",
                table: "ballots",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_proxies",
                table: "proxies",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicalAttendeeIdNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PhysicalAttendeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AttendanceType = table.Column<string>(type: "text", nullable: false),
                    AttendCode = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneSource = table.Column<string>(type: "text", nullable: true),
                    GiftReceived = table.Column<bool>(type: "boolean", nullable: false),
                    GiftReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GiftReceivedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PosTerminal = table.Column<string>(type: "text", nullable: true),
                    OperatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_records_meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_records_shareholders_ShareholderId",
                        column: x => x.ShareholderId,
                        principalTable: "shareholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalAttendingShareholders = table.Column<int>(type: "integer", nullable: false),
                    TotalAttendingShares = table.Column<long>(type: "bigint", nullable: false),
                    PercentageQuorum = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalPhysicalAttendees = table.Column<int>(type: "integer", nullable: false),
                    TotalBallotsIssued = table.Column<int>(type: "integer", nullable: false),
                    SnapshotAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_snapshots_meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "election_votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<long>(type: "bigint", nullable: false),
                    EnteredBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_election_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_election_votes_ballots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "ballots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_election_votes_meeting_candidates_MeetingCandidateId",
                        column: x => x.MeetingCandidateId,
                        principalTable: "meeting_candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meeting_template_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateType = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meeting_template_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meeting_template_configs_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meeting_template_configs_meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proxy_recipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Organization = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhoneUpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proxy_recipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tally_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalBallotIssued = table.Column<int>(type: "integer", nullable: false),
                    TotalBallotCounted = table.Column<int>(type: "integer", nullable: false),
                    TotalBallotNotReturned = table.Column<int>(type: "integer", nullable: false),
                    TotalBallotInvalid = table.Column<int>(type: "integer", nullable: false),
                    DenominatorShares = table.Column<long>(type: "bigint", nullable: false),
                    ConfirmedBy1 = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedBy2 = table.Column<Guid>(type: "uuid", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tally_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tally_snapshots_meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vote_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingResolutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoteChoice = table.Column<string>(type: "text", nullable: false),
                    VotingShares = table.Column<long>(type: "bigint", nullable: false),
                    BulkApproved = table.Column<bool>(type: "boolean", nullable: false),
                    EnteredBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vote_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vote_results_ballots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "ballots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vote_results_meeting_resolutions_MeetingResolutionId",
                        column: x => x.MeetingResolutionId,
                        principalTable: "meeting_resolutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ballot_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    BallotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceShareholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ballot_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ballot_groups_attendance_records_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalTable: "attendance_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ballot_groups_ballots_BallotId",
                        column: x => x.BallotId,
                        principalTable: "ballots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ballot_groups_shareholders_SourceShareholderId",
                        column: x => x.SourceShareholderId,
                        principalTable: "shareholders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_proxies_GranteeRecipientId",
                table: "proxies",
                column: "GranteeRecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_proxies_GranteeShareholderId",
                table: "proxies",
                column: "GranteeShareholderId");

            migrationBuilder.CreateIndex(
                name: "IX_proxies_MeetingId_GrantorId",
                table: "proxies",
                columns: new[] { "MeetingId", "GrantorId" });

            migrationBuilder.CreateIndex(
                name: "IX_proxies_Status",
                table: "proxies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_proxies_SupersededById",
                table: "proxies",
                column: "SupersededById");

            migrationBuilder.CreateIndex(
                name: "IX_ballots_AttendanceRecordId_BallotType",
                table: "ballots",
                columns: new[] { "AttendanceRecordId", "BallotType" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_AttendCode",
                table: "attendance_records",
                column: "AttendCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_MeetingId_ShareholderId",
                table: "attendance_records",
                columns: new[] { "MeetingId", "ShareholderId" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_PhysicalAttendeeIdNumber",
                table: "attendance_records",
                column: "PhysicalAttendeeIdNumber");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_ShareholderId",
                table: "attendance_records",
                column: "ShareholderId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_snapshots_MeetingId_SnapshotType",
                table: "attendance_snapshots",
                columns: new[] { "MeetingId", "SnapshotType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ballot_groups_AttendanceRecordId_SourceShareholderId",
                table: "ballot_groups",
                columns: new[] { "AttendanceRecordId", "SourceShareholderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ballot_groups_BallotId",
                table: "ballot_groups",
                column: "BallotId");

            migrationBuilder.CreateIndex(
                name: "IX_ballot_groups_SourceShareholderId",
                table: "ballot_groups",
                column: "SourceShareholderId");

            migrationBuilder.CreateIndex(
                name: "IX_election_votes_BallotId_MeetingCandidateId",
                table: "election_votes",
                columns: new[] { "BallotId", "MeetingCandidateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_election_votes_MeetingCandidateId",
                table: "election_votes",
                column: "MeetingCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_meeting_template_configs_MeetingId_TemplateType",
                table: "meeting_template_configs",
                columns: new[] { "MeetingId", "TemplateType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meeting_template_configs_TemplateId",
                table: "meeting_template_configs",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_proxy_recipients_IdNumber",
                table: "proxy_recipients",
                column: "IdNumber");

            migrationBuilder.CreateIndex(
                name: "IX_tally_snapshots_MeetingId_SnapshotType",
                table: "tally_snapshots",
                columns: new[] { "MeetingId", "SnapshotType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vote_results_BallotId_MeetingResolutionId",
                table: "vote_results",
                columns: new[] { "BallotId", "MeetingResolutionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vote_results_MeetingResolutionId",
                table: "vote_results",
                column: "MeetingResolutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ballots_attendance_records_AttendanceRecordId",
                table: "ballots",
                column: "AttendanceRecordId",
                principalTable: "attendance_records",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proxies_meetings_MeetingId",
                table: "proxies",
                column: "MeetingId",
                principalTable: "meetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proxies_proxies_SupersededById",
                table: "proxies",
                column: "SupersededById",
                principalTable: "proxies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_proxies_proxy_recipients_GranteeRecipientId",
                table: "proxies",
                column: "GranteeRecipientId",
                principalTable: "proxy_recipients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proxies_shareholders_GranteeShareholderId",
                table: "proxies",
                column: "GranteeShareholderId",
                principalTable: "shareholders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proxies_shareholders_GrantorId",
                table: "proxies",
                column: "GrantorId",
                principalTable: "shareholders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ballots_attendance_records_AttendanceRecordId",
                table: "ballots");

            migrationBuilder.DropForeignKey(
                name: "FK_proxies_meetings_MeetingId",
                table: "proxies");

            migrationBuilder.DropForeignKey(
                name: "FK_proxies_proxies_SupersededById",
                table: "proxies");

            migrationBuilder.DropForeignKey(
                name: "FK_proxies_proxy_recipients_GranteeRecipientId",
                table: "proxies");

            migrationBuilder.DropForeignKey(
                name: "FK_proxies_shareholders_GranteeShareholderId",
                table: "proxies");

            migrationBuilder.DropForeignKey(
                name: "FK_proxies_shareholders_GrantorId",
                table: "proxies");

            migrationBuilder.DropTable(
                name: "attendance_snapshots");

            migrationBuilder.DropTable(
                name: "ballot_groups");

            migrationBuilder.DropTable(
                name: "election_votes");

            migrationBuilder.DropTable(
                name: "meeting_template_configs");

            migrationBuilder.DropTable(
                name: "proxy_recipients");

            migrationBuilder.DropTable(
                name: "tally_snapshots");

            migrationBuilder.DropTable(
                name: "vote_results");

            migrationBuilder.DropTable(
                name: "attendance_records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_proxies",
                table: "proxies");

            migrationBuilder.DropIndex(
                name: "IX_proxies_GranteeRecipientId",
                table: "proxies");

            migrationBuilder.DropIndex(
                name: "IX_proxies_GranteeShareholderId",
                table: "proxies");

            migrationBuilder.DropIndex(
                name: "IX_proxies_MeetingId_GrantorId",
                table: "proxies");

            migrationBuilder.DropIndex(
                name: "IX_proxies_Status",
                table: "proxies");

            migrationBuilder.DropIndex(
                name: "IX_proxies_SupersededById",
                table: "proxies");

            migrationBuilder.DropIndex(
                name: "IX_ballots_AttendanceRecordId_BallotType",
                table: "ballots");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "proxies");

            migrationBuilder.DropColumn(
                name: "GranteeRecipientId",
                table: "proxies");

            migrationBuilder.DropColumn(
                name: "GranteeShareholderId",
                table: "proxies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "proxies");

            migrationBuilder.DropColumn(
                name: "SupersededById",
                table: "proxies");

            migrationBuilder.DropColumn(
                name: "DefaultPrintMode",
                table: "meetings");

            migrationBuilder.DropColumn(
                name: "GiftEnabled",
                table: "meetings");

            migrationBuilder.DropColumn(
                name: "QuorumThreshold",
                table: "meetings");

            migrationBuilder.DropColumn(
                name: "ApprovalThreshold",
                table: "meeting_resolutions");

            migrationBuilder.DropColumn(
                name: "ResolutionType",
                table: "meeting_resolutions");

            migrationBuilder.DropColumn(
                name: "CandidateBoard",
                table: "meeting_candidates");

            migrationBuilder.DropColumn(
                name: "NumberOfSeats",
                table: "meeting_candidates");

            migrationBuilder.DropColumn(
                name: "AttendanceRecordId",
                table: "ballots");

            migrationBuilder.DropColumn(
                name: "BallotType",
                table: "ballots");

            migrationBuilder.DropColumn(
                name: "BulkApproved",
                table: "ballots");

            migrationBuilder.DropColumn(
                name: "IsSplitBallot",
                table: "ballots");

            migrationBuilder.DropColumn(
                name: "ProxyRepresentationNote",
                table: "ballots");

            migrationBuilder.DropColumn(
                name: "SplitSequence",
                table: "ballots");

            migrationBuilder.RenameTable(
                name: "proxies",
                newName: "Proxies");

            migrationBuilder.RenameIndex(
                name: "IX_proxies_GrantorId",
                table: "Proxies",
                newName: "IX_Proxies_GrantorId");

            migrationBuilder.AlterColumn<int>(
                name: "Scope",
                table: "Proxies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "ProxyType",
                table: "Proxies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "GranteeName",
                table: "Proxies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "GranteeIdNumber",
                table: "Proxies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Proxies",
                table: "Proxies",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Proxies_MeetingId",
                table: "Proxies",
                column: "MeetingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proxies_meetings_MeetingId",
                table: "Proxies",
                column: "MeetingId",
                principalTable: "meetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proxies_shareholders_GrantorId",
                table: "Proxies",
                column: "GrantorId",
                principalTable: "shareholders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
