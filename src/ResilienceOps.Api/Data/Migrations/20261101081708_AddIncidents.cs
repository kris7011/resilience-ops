using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResilienceOps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RiskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ResponsiblePerson = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReportedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    InvestigatingUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    ResolvedUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    ClosedUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    UpdatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IncidentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NewStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OccurredUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentHistory_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentMitigations",
                columns: table => new
                {
                    IncidentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MitigationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentMitigations", x => new { x.IncidentId, x.MitigationId });
                    table.ForeignKey(
                        name: "FK_IncidentMitigations_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentMitigations_MitigationActions_MitigationId",
                        column: x => x.MitigationId,
                        principalTable: "MitigationActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentHistory_IncidentId_Sequence",
                table: "IncidentHistory",
                columns: new[] { "IncidentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentMitigations_MitigationId",
                table: "IncidentMitigations",
                column: "MitigationId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_RiskId_ReportedUtc",
                table: "Incidents",
                columns: new[] { "RiskId", "ReportedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_RiskId_Status",
                table: "Incidents",
                columns: new[] { "RiskId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentHistory");

            migrationBuilder.DropTable(
                name: "IncidentMitigations");

            migrationBuilder.DropTable(
                name: "Incidents");
        }
    }
}
