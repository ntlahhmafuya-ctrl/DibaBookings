/*
 * DATABASE SETUP: PrivacyRequests
 * Responsibility: create the table used to track user privacy requests.
 * Run this script once against the same SQL Server database used by DIBA Bookings.
 * This script does not delete or modify existing account, booking, or payment records.
 */
IF OBJECT_ID(N'dbo.PrivacyRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrivacyRequests
    (
        PrivacyRequestId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_PrivacyRequests PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        RequestType NVARCHAR(40) NOT NULL,
        Description NVARCHAR(2000) NOT NULL,
        Status NVARCHAR(40) NOT NULL
            CONSTRAINT DF_PrivacyRequests_Status DEFAULT N'Submitted',
        SubmittedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_PrivacyRequests_SubmittedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_PrivacyRequests_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        Response NVARCHAR(2000) NULL,
        ReviewedByUserId UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_PrivacyRequests_Users_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
            ON DELETE NO ACTION
    );

    CREATE INDEX IX_PrivacyRequests_UserId_SubmittedAtUtc
        ON dbo.PrivacyRequests(UserId, SubmittedAtUtc DESC);

    CREATE INDEX IX_PrivacyRequests_Status_SubmittedAtUtc
        ON dbo.PrivacyRequests(Status, SubmittedAtUtc);
END;
