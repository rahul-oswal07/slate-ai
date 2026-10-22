using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LessonVoiceMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "Lessons",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "ExamplesUsed",
                table: "Lessons",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]); // existing lessons get an empty list

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MappingStartedAt",
                table: "Lessons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryEn",
                table: "Lessons",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "ExamplesUsed",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "MappingStartedAt",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "SummaryEn",
                table: "Lessons");
        }
    }
}
