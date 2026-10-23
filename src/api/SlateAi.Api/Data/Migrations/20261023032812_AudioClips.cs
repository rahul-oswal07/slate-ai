using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AudioClips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioSegments",
                table: "CatchUpPackages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AudioClips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: true),
                    TextHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Bytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    Seconds = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioClips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioClips_CatchUpPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "CatchUpPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioClips_Kind_Language_TextHash",
                table: "AudioClips",
                columns: new[] { "Kind", "Language", "TextHash" },
                unique: true,
                filter: "\"TextHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AudioClips_PackageId",
                table: "AudioClips",
                column: "PackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AudioClips");

            migrationBuilder.DropColumn(
                name: "AudioSegments",
                table: "CatchUpPackages");
        }
    }
}
