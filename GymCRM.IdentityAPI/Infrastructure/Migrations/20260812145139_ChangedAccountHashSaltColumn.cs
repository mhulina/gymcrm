using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymCRM.IdentityAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangedAccountHashSaltColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HashSalt",
                schema: "identity_db",
                table: "Accounts",
                newName: "HashPepper");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HashPepper",
                schema: "identity_db",
                table: "Accounts",
                newName: "HashSalt");
        }
    }
}
