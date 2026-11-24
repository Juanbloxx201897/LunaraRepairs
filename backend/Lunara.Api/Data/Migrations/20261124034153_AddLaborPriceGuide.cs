using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborPriceGuide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LaborWorkType",
                table: "RepairOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: "Trabajo personalizado");

            migrationBuilder.CreateTable(
                name: "LaborPriceGuideEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: false),
                    Brand = table.Column<string>(type: "TEXT", nullable: false),
                    WorkType = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborPriceGuideEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaborPriceGuideEntries_DeviceType_Brand_WorkType",
                table: "LaborPriceGuideEntries",
                columns: new[] { "DeviceType", "Brand", "WorkType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaborPriceGuideEntries");

            migrationBuilder.DropColumn(
                name: "LaborWorkType",
                table: "RepairOrders");
        }
    }
}
