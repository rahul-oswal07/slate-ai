using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class GreetingClips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId",
                table: "AudioClips",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AudioClips_TeacherId",
                table: "AudioClips",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_AudioClips_Teachers_TeacherId",
                table: "AudioClips",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AudioClips_Teachers_TeacherId",
                table: "AudioClips");

            migrationBuilder.DropIndex(
                name: "IX_AudioClips_TeacherId",
                table: "AudioClips");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "AudioClips");
        }
    }
}
