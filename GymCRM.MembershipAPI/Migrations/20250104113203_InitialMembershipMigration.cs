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
                values: new object[] { new Guid("ae752a60-62af-4e15-96b2-5fdf6bd56193"), new DateTime(2025, 1, 4, 11, 32, 2, 691, DateTimeKind.Utc).AddTicks(1548), "test@test.com", "9722DA86D6BC3C60C568BFC68", "AiNm+N8wEcu8a9r5057grIbrzvH4QKUB/VuIMpgtRHw=", 1 });

            migrationBuilder.InsertData(
                table: "Members",
                columns: new[] { "Id", "AccountGuid", "AccountType", "Email", "FirstName", "Gender", "GymSubscriptionType", "LastName", "MiddleName", "MobileNumber", "PersonalTrainerId", "PhoneNumber", "WorkingExperienceInMonths", "WorkoutGroupIds" },
                values: new object[] { 1, new Guid("ae752a60-62af-4e15-96b2-5fdf6bd56193"), 1, "test@test.com", null, 1, 1, null, null, null, null, null, null, null });

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
