using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lunara.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MarkLaborPriceGuideSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSample",
                table: "LaborPriceGuideEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSample",
                table: "LaborPriceGuideEntries");
        }
    }
}
