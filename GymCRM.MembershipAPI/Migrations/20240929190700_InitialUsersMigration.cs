using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GymCRM.MembershipAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialUsersMigration : Migration
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
                    WorkoutGroupId = table.Column<int>(type: "integer", nullable: true),
                    Guid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymUsers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "GymUsers",
                columns: new[] { "Id", "DateJoined", "Email", "FirstName", "Guid", "LastName", "MiddleName", "MobilePhone", "PersonalTrainerId", "PhoneNumber", "UserType", "WorkoutGroupId" },
                values: new object[] { 1, new DateTime(2024, 9, 28, 22, 0, 0, 0, DateTimeKind.Utc), "test@test.com", "Admin", new Guid("7402413a-6032-45a3-8283-031c93a600b1"), "Adminski", null, null, null, "123456789", 1, null });

            migrationBuilder.CreateIndex(
                name: "IX_Guid",
                table: "GymUsers",
                column: "Guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymUsers");
        }
    }
}
