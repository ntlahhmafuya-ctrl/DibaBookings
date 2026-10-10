/*
 DIBA Bookings - venue photo storage
 Run this script against the SQL Server database used by DIBA_Backend.
 It is safe to run more than once.
*/
IF COL_LENGTH('dbo.Venues', 'VenueImageData') IS NULL
BEGIN
    ALTER TABLE dbo.Venues
    ADD VenueImageData NVARCHAR(MAX) NULL;
END;
