using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyStarMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ParentDeliveries_OneSource",
                table: "ParentDeliveries");

            migrationBuilder.AddColumn<Guid>(
                name: "WeeklyStarMessageId",
                table: "ParentDeliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WeeklyStarApprovals",
                columns: table => new
                {
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedByTeacherId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyStarApprovals", x => x.ClassId);
                    table.ForeignKey(
                        name: "FK_WeeklyStarApprovals_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyStarApprovals_Teachers_ApprovedByTeacherId",
                        column: x => x.ApprovedByTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyStarMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekOf = table.Column<DateOnly>(type: "date", nullable: false),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MessageText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BadgeText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Token = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyStarMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyStarMessages_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyStarMessages_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_WeeklyStarMessageId",
                table: "ParentDeliveries",
                column: "WeeklyStarMessageId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParentDeliveries_OneSource",
                table: "ParentDeliveries",
                sql: "(CASE WHEN \"EveningMessageId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"CatchUpAssignmentId\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"WeeklyStarMessageId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyStarApprovals_ApprovedByTeacherId",
                table: "WeeklyStarApprovals",
                column: "ApprovedByTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyStarMessages_ChildId_WeekOf",
                table: "WeeklyStarMessages",
                columns: new[] { "ChildId", "WeekOf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyStarMessages_ClassId_WeekOf",
                table: "WeeklyStarMessages",
                columns: new[] { "ClassId", "WeekOf" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyStarMessages_Token",
                table: "WeeklyStarMessages",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ParentDeliveries_WeeklyStarMessages_WeeklyStarMessageId",
                table: "ParentDeliveries",
                column: "WeeklyStarMessageId",
                principalTable: "WeeklyStarMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParentDeliveries_WeeklyStarMessages_WeeklyStarMessageId",
                table: "ParentDeliveries");

            migrationBuilder.DropTable(
                name: "WeeklyStarApprovals");

            migrationBuilder.DropTable(
                name: "WeeklyStarMessages");

            migrationBuilder.DropIndex(
                name: "IX_ParentDeliveries_WeeklyStarMessageId",
                table: "ParentDeliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ParentDeliveries_OneSource",
                table: "ParentDeliveries");

            migrationBuilder.DropColumn(
                name: "WeeklyStarMessageId",
                table: "ParentDeliveries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ParentDeliveries_OneSource",
                table: "ParentDeliveries",
                sql: "(\"EveningMessageId\" IS NULL) <> (\"CatchUpAssignmentId\" IS NULL)");
        }
    }
}
