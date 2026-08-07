using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCRM.IdentityAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberPhotoDateOfBirthAndHourlyPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                schema: "identity_db",
                table: "Members",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyPrice",
                schema: "identity_db",
                table: "Members",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Photo",
                schema: "identity_db",
                table: "Members",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                schema: "identity_db",
                table: "Members",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                schema: "identity_db",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "HourlyPrice",
                schema: "identity_db",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Photo",
                schema: "identity_db",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                schema: "identity_db",
                table: "Members");
        }
    }
}
