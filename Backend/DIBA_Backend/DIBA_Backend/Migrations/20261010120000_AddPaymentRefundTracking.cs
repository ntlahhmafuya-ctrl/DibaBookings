using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_Backend.Migrations
{
    public partial class AddPaymentRefundTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guard each addition because some local databases have schema
            // changes that were applied manually before EF recorded a migration.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Payments', N'RefundAmount') IS NULL
    ALTER TABLE [dbo].[Payments] ADD [RefundAmount] decimal(18,2) NULL;
IF COL_LENGTH(N'dbo.Payments', N'RefundReason') IS NULL
    ALTER TABLE [dbo].[Payments] ADD [RefundReason] nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.Payments', N'RefundStatus') IS NULL
    ALTER TABLE [dbo].[Payments] ADD [RefundStatus] nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.Payments', N'RefundRequestedAtUtc') IS NULL
    ALTER TABLE [dbo].[Payments] ADD [RefundRequestedAtUtc] datetime2 NULL;
IF COL_LENGTH(N'dbo.Payments', N'RefundProcessedAtUtc') IS NULL
    ALTER TABLE [dbo].[Payments] ADD [RefundProcessedAtUtc] datetime2 NULL;
IF COL_LENGTH(N'dbo.Payments', N'YocoRefundId') IS NULL
    ALTER TABLE [dbo].[Payments] ADD [YocoRefundId] nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.Payments', N'RefundFailureReason') IS NULL
    ALTER TABLE [dbo].[Payments] ADD [RefundFailureReason] nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.Payments', N'RefundRequestKey') IS NULL
    ALTER TABLE [dbo].[Payments] ADD [RefundRequestKey] nvarchar(max) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.Payments', N'RefundRequestKey') IS NOT NULL
    ALTER TABLE [dbo].[Payments] DROP COLUMN [RefundRequestKey];
IF COL_LENGTH(N'dbo.Payments', N'RefundFailureReason') IS NOT NULL
    ALTER TABLE [dbo].[Payments] DROP COLUMN [RefundFailureReason];
IF COL_LENGTH(N'dbo.Payments', N'YocoRefundId') IS NOT NULL
    ALTER TABLE [dbo].[Payments] DROP COLUMN [YocoRefundId];
IF COL_LENGTH(N'dbo.Payments', N'RefundProcessedAtUtc') IS NOT NULL
    ALTER TABLE [dbo].[Payments] DROP COLUMN [RefundProcessedAtUtc];
IF COL_LENGTH(N'dbo.Payments', N'RefundRequestedAtUtc') IS NOT NULL
    ALTER TABLE [dbo].[Payments] DROP COLUMN [RefundRequestedAtUtc];
IF COL_LENGTH(N'dbo.Payments', N'RefundStatus') IS NOT NULL
    ALTER TABLE [dbo].[Payments] DROP COLUMN [RefundStatus];
IF COL_LENGTH(N'dbo.Payments', N'RefundReason') IS NOT NULL
    ALTER TABLE [dbo].[Payments] DROP COLUMN [RefundReason];
IF COL_LENGTH(N'dbo.Payments', N'RefundAmount') IS NOT NULL
    ALTER TABLE [dbo].[Payments] DROP COLUMN [RefundAmount];
");
        }
    }
}
