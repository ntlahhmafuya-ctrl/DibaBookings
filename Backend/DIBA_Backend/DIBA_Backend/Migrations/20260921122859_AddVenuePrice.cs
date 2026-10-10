using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_Backend.Migrations
{
    /// <summary>
    /// Adds the venue price column when it is not already present.
    /// This supports databases where the column was added manually before EF recorded this migration.
    /// </summary>
    public partial class AddVenuePrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Avoid a duplicate-column error when Venues.Price already exists.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Venues', N'Price') IS NULL
BEGIN
    ALTER TABLE [dbo].[Venues] ADD [Price] decimal(18,2) NOT NULL
        CONSTRAINT [DF_Venues_Price_Migration] DEFAULT (0.0);
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Review the target database before rolling back: an existing Price column
            // may predate this migration and may contain data that must be preserved.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Venues', N'Price') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Venues] DROP COLUMN [Price];
END");
        }
    }
}