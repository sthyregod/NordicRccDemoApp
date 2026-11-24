using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NordicRccDemoApp.Migrations;

/// <inheritdoc />
public partial class _20261123154321_UpdateSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DataPoint_TimeSeries_TimeSeriesId",
            table: "DataPoint");

        migrationBuilder.DropPrimaryKey(
            name: "PK_DataPoint",
            table: "DataPoint");

        migrationBuilder.RenameTable(
            name: "DataPoint",
            newName: "DataPoints");

        migrationBuilder.RenameIndex(
            name: "IX_DataPoint_TimeSeriesId",
            table: "DataPoints",
            newName: "IX_DataPoints_TimeSeriesId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_DataPoints",
            table: "DataPoints",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_DataPoints_TimeSeries_TimeSeriesId",
            table: "DataPoints",
            column: "TimeSeriesId",
            principalTable: "TimeSeries",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DataPoints_TimeSeries_TimeSeriesId",
            table: "DataPoints");

        migrationBuilder.DropPrimaryKey(
            name: "PK_DataPoints",
            table: "DataPoints");

        migrationBuilder.RenameTable(
            name: "DataPoints",
            newName: "DataPoint");

        migrationBuilder.RenameIndex(
            name: "IX_DataPoints_TimeSeriesId",
            table: "DataPoint",
            newName: "IX_DataPoint_TimeSeriesId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_DataPoint",
            table: "DataPoint",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_DataPoint_TimeSeries_TimeSeriesId",
            table: "DataPoint",
            column: "TimeSeriesId",
            principalTable: "TimeSeries",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
