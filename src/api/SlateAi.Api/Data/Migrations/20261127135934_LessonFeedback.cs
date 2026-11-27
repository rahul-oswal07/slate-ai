using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LessonFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MappingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PromptVersion = table.Column<int>(type: "integer", nullable: false),
                    MatchStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchedCompetencyIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: true),
                    ChosenCompetencyIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    ConfirmedVia = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SummaryKn = table.Column<string>(type: "text", nullable: true),
                    SummaryEn = table.Column<string>(type: "text", nullable: true),
                    ExamplesUsed = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonFeedback_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonFeedback_ClassId",
                table: "LessonFeedback",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonFeedback_CreatedAt",
                table: "LessonFeedback",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonFeedback");
        }
    }
}
