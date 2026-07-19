using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ResilienceOps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialRiskDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Risks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risks", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Risks",
                columns: new[] { "Id", "CreatedUtc", "Description", "Severity", "Status", "Title" },
                values: new object[,]
                {
                    { new Guid("82a22a45-56b0-437e-b65c-cc6d94e99e14"), 1784444700000L, "Unclear ownership may delay escalation of high-impact operational incidents.", "High", "Open", "Delayed incident escalation" },
                    { new Guid("e0894902-f420-4f75-bf42-d9678de1fb5d"), 1784444400000L, "Loss of the identity provider could prevent employees from accessing critical systems.", "Critical", "Open", "Identity provider outage" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Risks");
        }
    }
}
