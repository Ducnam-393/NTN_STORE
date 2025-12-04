using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NTN_STORE.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceiverName",
                table: "UserAddresses",
                newName: "Ward");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "UserAddresses",
                newName: "Province");

            migrationBuilder.RenameColumn(
                name: "AddressDetail",
                table: "UserAddresses",
                newName: "FullName");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "UserAddresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "UserAddresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "District",
                table: "UserAddresses");

            migrationBuilder.RenameColumn(
                name: "Ward",
                table: "UserAddresses",
                newName: "ReceiverName");

            migrationBuilder.RenameColumn(
                name: "Province",
                table: "UserAddresses",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "UserAddresses",
                newName: "AddressDetail");
        }
    }
}
