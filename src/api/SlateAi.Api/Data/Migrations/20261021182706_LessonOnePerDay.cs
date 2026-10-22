using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LessonOnePerDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_ClassId_Date",
                table: "Lessons");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ClassId_Date",
                table: "Lessons",
                columns: new[] { "ClassId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_ClassId_Date",
                table: "Lessons");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ClassId_Date",
                table: "Lessons",
                columns: new[] { "ClassId", "Date" });
        }
    }
}
