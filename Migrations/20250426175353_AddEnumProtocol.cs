using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnsProxy.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDoH",
                table: "Servers");

            migrationBuilder.RenameColumn(
                name: "UseWireFormat",
                table: "Servers",
                newName: "Protocol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Protocol",
                table: "Servers",
                newName: "UseWireFormat");

            migrationBuilder.AddColumn<bool>(
                name: "IsDoH",
                table: "Servers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
