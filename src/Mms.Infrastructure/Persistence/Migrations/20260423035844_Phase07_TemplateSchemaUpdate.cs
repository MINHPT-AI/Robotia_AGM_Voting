using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase07_TemplateSchemaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_meetings_MeetingId",
                table: "Templates");

            migrationBuilder.AlterColumn<Guid>(
                name: "MeetingId",
                table: "Templates",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Templates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Templates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_meetings_MeetingId",
                table: "Templates",
                column: "MeetingId",
                principalTable: "meetings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Templates_meetings_MeetingId",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Templates");

            migrationBuilder.AlterColumn<Guid>(
                name: "MeetingId",
                table: "Templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Templates_meetings_MeetingId",
                table: "Templates",
                column: "MeetingId",
                principalTable: "meetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
