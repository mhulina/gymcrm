using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GymCRM.MembershipAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialMembersMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HashedPassword = table.Column<string>(type: "varchar", nullable: false),
                    UserType = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "varchar", nullable: false),
                    MiddleName = table.Column<string>(type: "varchar", nullable: true),
                    LastName = table.Column<string>(type: "varchar", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "varchar", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar", nullable: false),
                    MobileNumber = table.Column<string>(type: "varchar", nullable: true),
                    DateJoined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PersonalTrainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkoutGroupIds = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    WorkingExperienceInMonths = table.Column<int>(type: "integer", nullable: true),
                    GymSubscriptionType = table.Column<int>(type: "integer", nullable: false),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Members",
                columns: new[] { "Id", "DateJoined", "Email", "FirstName", "Gender", "Guid", "GymSubscriptionType", "HashedPassword", "LastName", "MiddleName", "MobileNumber", "PersonalTrainerId", "PhoneNumber", "UserType", "WorkingExperienceInMonths", "WorkoutGroupIds" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 23, 0, 0, 0, DateTimeKind.Utc), "test@test.com", "Admin", 1, new Guid("955a2fa0-7e5c-4b1a-8d53-8c76aa154e74"), 0, "!#/)zW??C?JJ??", "Adminski", null, null, null, "123456789", 1, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Guid",
                table: "Members",
                column: "Guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Members");
        }
    }
}
