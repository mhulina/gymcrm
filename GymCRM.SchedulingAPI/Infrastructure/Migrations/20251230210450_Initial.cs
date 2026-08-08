using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCRM.SchedulingAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling_db");

            migrationBuilder.CreateTable(
                name: "Holidays",
                schema: "scheduling_db",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnglishName = table.Column<string>(type: "text", nullable: false),
                    LocalName = table.Column<string>(type: "text", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    RegionCode = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionTypes",
                schema: "scheduling_db",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeOff",
                schema: "scheduling_db",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeOff", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainerAvailabilities",
                schema: "scheduling_db",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkingWeekends = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DateCreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModifiedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerAvailabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                schema: "scheduling_db",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainerDailyAvailabilities",
                schema: "scheduling_db",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "text", nullable: false),
                    IsDayOff = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DateCreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModifiedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailabilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerDailyAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerDailyAvailabilities_TrainerAvailabilities_Availabili~",
                        column: x => x.AvailabilityId,
                        principalSchema: "scheduling_db",
                        principalTable: "TrainerAvailabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainerWorkingHours",
                schema: "scheduling_db",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DateCreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModifiedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DailyAvailabilityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerWorkingHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerWorkingHours_TrainerDailyAvailabilities_DailyAvailab~",
                        column: x => x.DailyAvailabilityId,
                        principalSchema: "scheduling_db",
                        principalTable: "TrainerDailyAvailabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerAvailabilities_TrainerId",
                schema: "scheduling_db",
                table: "TrainerAvailabilities",
                column: "TrainerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerDailyAvailabilities_AvailabilityId",
                schema: "scheduling_db",
                table: "TrainerDailyAvailabilities",
                column: "AvailabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerWorkingHours_DailyAvailabilityId",
                schema: "scheduling_db",
                table: "TrainerWorkingHours",
                column: "DailyAvailabilityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays",
                schema: "scheduling_db");

            migrationBuilder.DropTable(
                name: "SessionTypes",
                schema: "scheduling_db");

            migrationBuilder.DropTable(
                name: "TimeOff",
                schema: "scheduling_db");

            migrationBuilder.DropTable(
                name: "TrainerWorkingHours",
                schema: "scheduling_db");

            migrationBuilder.DropTable(
                name: "TrainingSessions",
                schema: "scheduling_db");

            migrationBuilder.DropTable(
                name: "TrainerDailyAvailabilities",
                schema: "scheduling_db");

            migrationBuilder.DropTable(
                name: "TrainerAvailabilities",
                schema: "scheduling_db");
        }
    }
}
