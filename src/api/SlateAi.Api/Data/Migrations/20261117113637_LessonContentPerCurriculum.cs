using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LessonContentPerCurriculum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonLinks",
                table: "LessonLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonContents",
                table: "LessonContents");

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumId",
                table: "LessonLinks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CurriculumId",
                table: "LessonContents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonLinks",
                table: "LessonLinks",
                columns: new[] { "CurriculumId", "CompetencyId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonContents",
                table: "LessonContents",
                columns: new[] { "CurriculumId", "CompetencyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonLinks",
                table: "LessonLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LessonContents",
                table: "LessonContents");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                table: "LessonLinks");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                table: "LessonContents");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonLinks",
                table: "LessonLinks",
                column: "CompetencyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LessonContents",
                table: "LessonContents",
                column: "CompetencyId");
        }
    }
}
