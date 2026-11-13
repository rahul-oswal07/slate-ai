using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlateAi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParentDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParentDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    EveningMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    CatchUpAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentDeliveries", x => x.Id);
                    table.CheckConstraint("CK_ParentDeliveries_OneSource", "(\"EveningMessageId\" IS NULL) <> (\"CatchUpAssignmentId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_ParentDeliveries_CatchUpAssignments_CatchUpAssignmentId",
                        column: x => x.CatchUpAssignmentId,
                        principalTable: "CatchUpAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentDeliveries_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentDeliveries_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentDeliveries_EveningMessages_EveningMessageId",
                        column: x => x.EveningMessageId,
                        principalTable: "EveningMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_CatchUpAssignmentId",
                table: "ParentDeliveries",
                column: "CatchUpAssignmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_ChildId",
                table: "ParentDeliveries",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_ClassId",
                table: "ParentDeliveries",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_EveningMessageId",
                table: "ParentDeliveries",
                column: "EveningMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParentDeliveries_Status",
                table: "ParentDeliveries",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParentDeliveries");
        }
    }
}
