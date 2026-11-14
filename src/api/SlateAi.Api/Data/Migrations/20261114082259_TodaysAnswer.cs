using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TodaysAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PictureAnsweredAt",
                table: "EveningMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PictureFirst",
                table: "EveningMessages",
                type: "character varying(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PictureSecond",
                table: "EveningMessages",
                type: "character varying(1)",
                maxLength: 1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PictureAnsweredAt",
                table: "EveningMessages");

            migrationBuilder.DropColumn(
                name: "PictureFirst",
                table: "EveningMessages");

            migrationBuilder.DropColumn(
                name: "PictureSecond",
                table: "EveningMessages");
        }
    }
}
