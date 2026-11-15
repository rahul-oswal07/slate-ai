using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CheckResponseAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "CheckResponses",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "CheckResponses");
        }
    }
}
