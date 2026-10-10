using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_Backend.Migrations
{
    /// <summary>
    /// Adds geographic coordinates to venues when the columns are not already present.
    /// This guards databases where the columns were added manually before EF recorded the migration.
    /// </summary>
    public partial class AddVenueCoordinates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Latitude only when it is missing, avoiding duplicate-column failures
            // on databases that already contain this column.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Venues', N'Latitude') IS NULL
BEGIN
    ALTER TABLE [dbo].[Venues] ADD [Latitude] float NOT NULL
        CONSTRAINT [DF_Venues_Latitude_Migration] DEFAULT (0.0);
END");

            // Add Longitude only when it is missing, avoiding duplicate-column failures
            // on databases that already contain this column.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Venues', N'Longitude') IS NULL
BEGIN
    ALTER TABLE [dbo].[Venues] ADD [Longitude] float NOT NULL
        CONSTRAINT [DF_Venues_Longitude_Migration] DEFAULT (0.0);
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Roll back only if the columns exist. Do not run database rollback commands
            // against a database with manually managed venue coordinates without reviewing
            // whether those columns contain data that must be preserved.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Venues', N'Longitude') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Venues] DROP COLUMN [Longitude];
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Venues', N'Latitude') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Venues] DROP COLUMN [Latitude];
END");
        }
    }
}