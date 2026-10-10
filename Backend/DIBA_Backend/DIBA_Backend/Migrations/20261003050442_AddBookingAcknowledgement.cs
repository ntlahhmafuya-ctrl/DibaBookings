using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingAcknowledgement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Some databases may already contain these columns even though this
            // migration has not yet been recorded in __EFMigrationsHistory.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Bookings', N'AcknowledgementAccepted') IS NULL
BEGIN
    ALTER TABLE [dbo].[Bookings] ADD [AcknowledgementAccepted] bit NOT NULL
        CONSTRAINT [DF_Bookings_AcknowledgementAccepted_Migration] DEFAULT (0);
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Bookings', N'AcknowledgementAcceptedAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[Bookings] ADD [AcknowledgementAcceptedAt] datetime2 NULL;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only use this rollback on databases where these columns were
            // originally introduced by this migration.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Bookings', N'AcknowledgementAcceptedAt') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Bookings] DROP COLUMN [AcknowledgementAcceptedAt];
END");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Bookings', N'AcknowledgementAccepted') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Bookings] DROP COLUMN [AcknowledgementAccepted];
END");
        }
    }
}