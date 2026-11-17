using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TextbookImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Extras",
                table: "CurriculumLessons",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "Flags",
                table: "CurriculumLessons",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "CurriculumLessons",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "reviewed");

            migrationBuilder.AddColumn<Guid>(
                name: "ImportId",
                table: "Curricula",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurriculumImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Medium = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Book = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Part = table.Column<int>(type: "integer", nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StepsDone = table.Column<int>(type: "integer", nullable: false),
                    StepsTotal = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string[]>(type: "text[]", nullable: false),
                    CurriculumId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumImports_Curricula_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curricula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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

            migrationBuilder.CreateTable(
                name: "CurriculumImportSteps",
                columns: table => new
                {
                    ImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Result = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumImportSteps", x => new { x.ImportId, x.Key });
                    table.ForeignKey(
                        name: "FK_CurriculumImportSteps_CurriculumImports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "CurriculumImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumImports_ClientRequestId",
                table: "CurriculumImports",
                column: "ClientRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumImports_CurriculumId",
                table: "CurriculumImports",
                column: "CurriculumId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumImportPages");

            migrationBuilder.DropTable(
                name: "CurriculumImportSteps");

            migrationBuilder.DropTable(
                name: "CurriculumImports");

            migrationBuilder.DropColumn(
                name: "Extras",
                table: "CurriculumLessons");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "CurriculumLessons");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "CurriculumLessons");

            migrationBuilder.DropColumn(
                name: "ImportId",
                table: "Curricula");
        }
    }
}
