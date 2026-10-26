using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LessonAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LessonAudioId",
                table: "EveningMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transcript",
                table: "AudioClips",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LessonAudios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    ClipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Verdict = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByTeacherId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedClipId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonAudios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonAudios_AudioClips_ApprovedClipId",
                        column: x => x.ApprovedClipId,
                        principalTable: "AudioClips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LessonAudios_AudioClips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "AudioClips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LessonAudios_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EveningMessages_LessonAudioId",
                table: "EveningMessages",
                column: "LessonAudioId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonAudios_ApprovedClipId",
                table: "LessonAudios",
                column: "ApprovedClipId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonAudios_ClassId_Date_Language",
                table: "LessonAudios",
                columns: new[] { "ClassId", "Date", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonAudios_ClipId",
                table: "LessonAudios",
                column: "ClipId");

            migrationBuilder.AddForeignKey(
                name: "FK_EveningMessages_LessonAudios_LessonAudioId",
                table: "EveningMessages",
                column: "LessonAudioId",
                principalTable: "LessonAudios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EveningMessages_LessonAudios_LessonAudioId",
                table: "EveningMessages");

            migrationBuilder.DropTable(
                name: "LessonAudios");

            migrationBuilder.DropIndex(
                name: "IX_EveningMessages_LessonAudioId",
                table: "EveningMessages");

            migrationBuilder.DropColumn(
                name: "LessonAudioId",
                table: "EveningMessages");

            migrationBuilder.DropColumn(
                name: "Transcript",
                table: "AudioClips");
        }
    }
}
