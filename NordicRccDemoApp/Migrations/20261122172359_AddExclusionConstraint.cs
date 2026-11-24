using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NordicRccDemoApp.Migrations;

/// <inheritdoc />
public partial class _20261122172359_AddExclusionConstraint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
        
        migrationBuilder.Sql(@"
            ALTER TABLE ""DataPoint""
            ADD CONSTRAINT ""NoDpOverlap""
            EXCLUDE USING gist (
                ""TimeSeriesId"" WITH =,
                ""Period""         WITH &&
            );
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"ALTER TABLE ""DataPoint"" DROP CONSTRAINT ""NoDpOverlap"";");
    }
}
