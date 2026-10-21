using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CorrectedAt",
                table: "Attendance",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceivedAt",
                table: "Attendance",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Attendance",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<bool>(
                name: "SourcePresent",
                table: "Attendance",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectedAt",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "SourcePresent",
                table: "Attendance");
        }
    }
}
