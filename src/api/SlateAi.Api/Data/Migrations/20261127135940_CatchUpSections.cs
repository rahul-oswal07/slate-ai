using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatchUpSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "RemovedSections",
                table: "CatchUpPackages",
                type: "text[]",
                nullable: false,
                // Existing catch-ups have nothing removed.
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.AddColumn<bool>(
                name: "TeacherEdited",
                table: "CatchUpPackages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TeacherNote",
                table: "CatchUpPackages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "CatchUpPackages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemovedSections",
                table: "CatchUpPackages");

            migrationBuilder.DropColumn(
                name: "TeacherEdited",
                table: "CatchUpPackages");

            migrationBuilder.DropColumn(
                name: "TeacherNote",
                table: "CatchUpPackages");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "CatchUpPackages");
        }
    }
}
