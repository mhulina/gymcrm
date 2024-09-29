using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GymCRM.UsersAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersToDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserType = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    MiddleName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    MobilePhone = table.Column<string>(type: "text", nullable: true),
                    DateJoined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PersonalTrainerId = table.Column<int>(type: "integer", nullable: true),
                    WorkoutGroupId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymUsers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "GymUsers",
                columns: new[] { "Id", "DateJoined", "Email", "FirstName", "LastName", "MiddleName", "MobilePhone", "PersonalTrainerId", "PhoneNumber", "UserType", "WorkoutGroupId" },
                values: new object[] { 1, new DateTime(2024, 9, 27, 22, 0, 0, 0, DateTimeKind.Utc), "test@test.com", "Admin", "Adminski", null, null, null, "123456789", 1, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymUsers");
        }
    }
}
