using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResilienceOps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMitigationActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MitigationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RiskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    DueDateUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitigationActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MitigationActions_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MitigationActions_RiskId",
                table: "MitigationActions",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_MitigationActions_RiskId_Status",
                table: "MitigationActions",
                columns: new[] { "RiskId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MitigationActions");
        }
    }
}
