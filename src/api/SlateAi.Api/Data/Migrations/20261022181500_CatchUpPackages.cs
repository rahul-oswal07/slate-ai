using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatchUpPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatchUpPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekOf = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetCompetencyIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    TargetKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScriptVersion = table.Column<int>(type: "integer", nullable: false),
                    PromptVersion = table.Column<int>(type: "integer", nullable: true),
                    Hint = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Verdict = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Script = table.Column<string>(type: "text", nullable: true),
                    KeyWordsKn = table.Column<List<string>>(type: "text[]", nullable: false),
                    GlossKn = table.Column<string>(type: "text", nullable: true),
                    GlossEn = table.Column<string>(type: "text", nullable: true),
                    Activity = table.Column<string>(type: "jsonb", nullable: false),
                    CheckItem = table.Column<string>(type: "jsonb", nullable: false),
                    AudioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatchUpPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatchUpPackages_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatchUpAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekOf = table.Column<DateOnly>(type: "date", nullable: false),
                    MissedCompetencyIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    CompressionRationale = table.Column<string>(type: "jsonb", nullable: false),
                    Token = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatchUpAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatchUpAssignments_CatchUpPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "CatchUpPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatchUpAssignments_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatchUpAssignments_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatchUpAssignments_ChildId_WeekOf",
                table: "CatchUpAssignments",
                columns: new[] { "ChildId", "WeekOf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatchUpAssignments_ClassId_WeekOf",
                table: "CatchUpAssignments",
                columns: new[] { "ClassId", "WeekOf" });

            migrationBuilder.CreateIndex(
                name: "IX_CatchUpAssignments_PackageId",
                table: "CatchUpAssignments",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchUpAssignments_Token",
                table: "CatchUpAssignments",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatchUpPackages_ClassId_WeekOf_Language_TargetKey",
                table: "CatchUpPackages",
                columns: new[] { "ClassId", "WeekOf", "Language", "TargetKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatchUpAssignments");

            migrationBuilder.DropTable(
                name: "CatchUpPackages");
        }
    }
}
