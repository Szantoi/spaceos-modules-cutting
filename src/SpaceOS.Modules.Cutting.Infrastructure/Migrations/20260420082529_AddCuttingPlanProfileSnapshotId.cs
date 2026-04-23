using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCuttingPlanProfileSnapshotId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfileSnapshotId",
                schema: "spaceos_cutting",
                table: "CuttingPlans",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileSnapshotId",
                schema: "spaceos_cutting",
                table: "CuttingPlans");
        }
    }
}
