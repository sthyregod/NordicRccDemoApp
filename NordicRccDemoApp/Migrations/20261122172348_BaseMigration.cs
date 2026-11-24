using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace NordicRccDemoApp.Migrations;

/// <inheritdoc />
public partial class _20261122172348_BaseMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TimeSeries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BiddingZone = table.Column<string>(type: "text", nullable: false),
                Resolution = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TimeSeries", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "DataPoint",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TimeSeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                Period = table.Column<NpgsqlRange<DateTimeOffset>>(type: "tstzrange", nullable: false),
                value = table.Column<decimal>(type: "numeric", nullable: false),
                unit = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DataPoint", x => x.Id);
                table.ForeignKey(
                    name: "FK_DataPoint_TimeSeries_TimeSeriesId",
                    column: x => x.TimeSeriesId,
                    principalTable: "TimeSeries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DataPoint_TimeSeriesId",
            table: "DataPoint",
            column: "TimeSeriesId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DataPoint");

        migrationBuilder.DropTable(
            name: "TimeSeries");
    }
}
