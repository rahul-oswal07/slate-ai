using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatchUpApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "CatchUpPackages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByTeacherId",
                table: "CatchUpPackages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedScriptVersion",
                table: "CatchUpPackages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentAt",
                table: "CatchUpPackages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "CatchUpAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatchUpPackages_ApprovedByTeacherId",
                table: "CatchUpPackages",
                column: "ApprovedByTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_CatchUpPackages_Teachers_ApprovedByTeacherId",
                table: "CatchUpPackages",
                column: "ApprovedByTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatchUpPackages_Teachers_ApprovedByTeacherId",
                table: "CatchUpPackages");

            migrationBuilder.DropIndex(
                name: "IX_CatchUpPackages_ApprovedByTeacherId",
                table: "CatchUpPackages");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "CatchUpPackages");

            migrationBuilder.DropColumn(
                name: "ApprovedByTeacherId",
                table: "CatchUpPackages");

            migrationBuilder.DropColumn(
                name: "ApprovedScriptVersion",
                table: "CatchUpPackages");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "CatchUpPackages");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "CatchUpAssignments");
        }
    }
}
