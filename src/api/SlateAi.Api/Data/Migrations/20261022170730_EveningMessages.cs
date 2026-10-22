using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EveningMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Consent",
                table: "Children",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "no");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConsentChangedAt",
                table: "Children",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ParentStoppedAt",
                table: "Children",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PageFormat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PageFormatNoPart = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByTeacherId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageTemplates_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageTemplates_Teachers_ApprovedByTeacherId",
                        column: x => x.ApprovedByTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EveningMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    SlotValues = table.Column<string>(type: "jsonb", nullable: false),
                    Token = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EveningMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EveningMessages_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EveningMessages_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EveningMessages_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EveningMessages_MessageTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "MessageTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EveningMessages_ChildId_Date",
                table: "EveningMessages",
                columns: new[] { "ChildId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EveningMessages_ClassId_Date",
                table: "EveningMessages",
                columns: new[] { "ClassId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_EveningMessages_LessonId",
                table: "EveningMessages",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_EveningMessages_TemplateId",
                table: "EveningMessages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EveningMessages_Token",
                table: "EveningMessages",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_ApprovedByTeacherId",
                table: "MessageTemplates",
                column: "ApprovedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_ClassId_Language",
                table: "MessageTemplates",
                columns: new[] { "ClassId", "Language" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EveningMessages");

            migrationBuilder.DropTable(
                name: "MessageTemplates");

            migrationBuilder.DropColumn(
                name: "Consent",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "ConsentChangedAt",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "ParentStoppedAt",
                table: "Children");
        }
    }
}
