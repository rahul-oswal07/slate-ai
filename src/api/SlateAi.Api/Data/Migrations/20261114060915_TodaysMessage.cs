using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TodaysMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TodaysMessageId",
                table: "EveningMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TodaysMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CompetencyIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    Status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ApprovedVersion = table.Column<int>(type: "integer", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByTeacherId = table.Column<Guid>(type: "uuid", nullable: true),
                    Verdict = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Sections = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodaysMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodaysMessages_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodaysMessages_Teachers_ApprovedByTeacherId",
                        column: x => x.ApprovedByTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EveningMessages_TodaysMessageId",
                table: "EveningMessages",
                column: "TodaysMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TodaysMessages_ApprovedByTeacherId",
                table: "TodaysMessages",
                column: "ApprovedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TodaysMessages_ClassId_Date",
                table: "TodaysMessages",
                columns: new[] { "ClassId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EveningMessages_TodaysMessages_TodaysMessageId",
                table: "EveningMessages",
                column: "TodaysMessageId",
                principalTable: "TodaysMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EveningMessages_TodaysMessages_TodaysMessageId",
                table: "EveningMessages");

            migrationBuilder.DropTable(
                name: "TodaysMessages");

            migrationBuilder.DropIndex(
                name: "IX_EveningMessages_TodaysMessageId",
                table: "EveningMessages");

            migrationBuilder.DropColumn(
                name: "TodaysMessageId",
                table: "EveningMessages");
        }
    }
}
