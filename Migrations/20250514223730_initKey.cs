using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnsProxy.Migrations
{
    /// <inheritdoc />
    public partial class initKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rules_Servers_ForceServerId",
                table: "Rules");

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_Servers_ForceServerId",
                table: "Rules",
                column: "ForceServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rules_Servers_ForceServerId",
                table: "Rules");

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_Servers_ForceServerId",
                table: "Rules",
                column: "ForceServerId",
                principalTable: "Servers",
                principalColumn: "Id");
        }
    }
}
