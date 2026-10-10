using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_Backend.Migrations
{
    /// <summary>
    /// Creates the table used to track user privacy requests and administrator responses.
    /// It stores requests only; it does not delete user, booking, or payment records.
    /// </summary>
    public partial class AddPrivacyRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The repository also includes a guarded SQL setup script that may have
            // been run manually. Only create the table when it does not already exist.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.PrivacyRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrivacyRequests
    (
        PrivacyRequestId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_PrivacyRequests PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        RequestType NVARCHAR(MAX) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(MAX) NOT NULL,
        SubmittedAtUtc DATETIME2 NOT NULL,
        UpdatedAtUtc DATETIME2 NOT NULL,
        Response NVARCHAR(MAX) NULL,
        ReviewedByUserId UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_PrivacyRequests_Users_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE NO ACTION
    );

    CREATE INDEX IX_PrivacyRequests_UserId
        ON dbo.PrivacyRequests(UserId);
END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PrivacyRequests");
        }
    }
}
