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
            migrationBuilder.AddColumn<bool>(
                name: "AcknowledgementAccepted",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgementAcceptedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgementAccepted",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AcknowledgementAcceptedAt",
                table: "Bookings");
        }
    }
}
