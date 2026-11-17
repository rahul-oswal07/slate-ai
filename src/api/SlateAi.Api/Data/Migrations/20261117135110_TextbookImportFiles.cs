using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TextbookImportFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumImportPages");

            migrationBuilder.CreateTable(
                name: "CurriculumImportFiles",
                columns: table => new
                {
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Pdf = table.Column<byte[]>(type: "bytea", nullable: false),
                    PageSizes = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumImportFiles", x => x.ImportId);
                    table.ForeignKey(
                        name: "FK_CurriculumImportFiles_CurriculumImports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "CurriculumImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumImportFiles");

            migrationBuilder.CreateTable(
                name: "CurriculumImportPages",
                columns: table => new
                {
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Pdf = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumImportPages", x => new { x.ImportId, x.Number });
                    table.ForeignKey(
                        name: "FK_CurriculumImportPages_CurriculumImports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "CurriculumImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
