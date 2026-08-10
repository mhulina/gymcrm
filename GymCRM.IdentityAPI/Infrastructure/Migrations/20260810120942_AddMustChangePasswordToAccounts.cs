using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCRM.IdentityAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMustChangePasswordToAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                schema: "identity_db",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                schema: "identity_db",
                table: "Accounts");
        }
    }
}
