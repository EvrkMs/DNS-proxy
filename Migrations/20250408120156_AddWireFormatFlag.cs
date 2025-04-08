using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DNS_proxy.Migrations
{
    /// <inheritdoc />
    public partial class AddWireFormatFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseWireFormat",
                table: "DnsServers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseWireFormat",
                table: "DnsServers");
        }
    }
}
