using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GymCRM.MembershipAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialMembershipMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Email = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HashSalt = table.Column<string>(type: "text", nullable: false),
                    HashedPassword = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Guid);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    AccountType = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    MiddleName = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    LastName = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    MobileNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PersonalTrainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkoutGroupIds = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    WorkingExperienceInMonths = table.Column<int>(type: "integer", nullable: true),
                    GymSubscriptionType = table.Column<int>(type: "integer", nullable: false),
                    AccountGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Account_Members",
                        column: x => x.AccountGuid,
                        principalTable: "Accounts",
                        principalColumn: "Guid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Guid", "DateCreated", "Email", "HashSalt", "HashedPassword", "Id" },
                values: new object[] { new Guid("c1c826ab-5195-44a6-b3ce-b236b54daf78"), new DateTime(2025, 4, 7, 17, 11, 46, 865, DateTimeKind.Utc).AddTicks(3453), "test@test.com", "E84A81CE8413C73A3B7D96ECD", "9NMsR7hdMEjiE6wxjn69DgAVnER/5QUGHU8czqtEuK0=", 1 });

            migrationBuilder.InsertData(
                table: "Members",
                columns: new[] { "Id", "AccountGuid", "AccountType", "Email", "FirstName", "Gender", "GymSubscriptionType", "LastName", "MiddleName", "MobileNumber", "PersonalTrainerId", "PhoneNumber", "WorkingExperienceInMonths", "WorkoutGroupIds" },
                values: new object[] { 1, new Guid("c1c826ab-5195-44a6-b3ce-b236b54daf78"), 1, "test@test.com", null, 1, 1, null, null, null, null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Email",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guid",
                table: "Accounts",
                column: "Guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountGuid",
                table: "Members",
                column: "AccountGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Email1",
                table: "Members",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
