using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecondParentPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParentDeliveries_CatchUpAssignmentId",
                table: "ParentDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_ParentDeliveries_EveningMessageId",
                table: "ParentDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_ParentDeliveries_WeeklyStarMessageId",
                table: "ParentDeliveries");

            migrationBuilder.AddColumn<short>(
                name: "Recipient",
                table: "ParentDeliveries",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.AddColumn<string>(
                name: "ParentPhone2",
                table: "Children",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ParentPhone2SetAt",
                table: "Children",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_CatchUpAssignmentId_Recipient",
                table: "ParentDeliveries",
                columns: new[] { "CatchUpAssignmentId", "Recipient" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_EveningMessageId_Recipient",
                table: "ParentDeliveries",
                columns: new[] { "EveningMessageId", "Recipient" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_WeeklyStarMessageId_Recipient",
                table: "ParentDeliveries",
                columns: new[] { "WeeklyStarMessageId", "Recipient" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParentDeliveries_CatchUpAssignmentId_Recipient",
                table: "ParentDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_ParentDeliveries_EveningMessageId_Recipient",
                table: "ParentDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_ParentDeliveries_WeeklyStarMessageId_Recipient",
                table: "ParentDeliveries");

            migrationBuilder.DropColumn(
                name: "Recipient",
                table: "ParentDeliveries");

            migrationBuilder.DropColumn(
                name: "ParentPhone2",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "ParentPhone2SetAt",
                table: "Children");

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_CatchUpAssignmentId",
                table: "ParentDeliveries",
                column: "CatchUpAssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_EveningMessageId",
                table: "ParentDeliveries",
                column: "EveningMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_WeeklyStarMessageId",
                table: "ParentDeliveries",
                column: "WeeklyStarMessageId",
                unique: true);
        }
    }
}
