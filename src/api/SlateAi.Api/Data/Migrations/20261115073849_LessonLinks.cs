using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LessonLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonLinks",
                columns: table => new
                {
                    CompetencyId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonLinks", x => x.CompetencyId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonLinks");
        }
    }
}
