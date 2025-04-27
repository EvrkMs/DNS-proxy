using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnsProxy.Migrations
{
    /// <inheritdoc />
    public partial class AddForceUpstreamKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForceUpstream",
                table: "Rules");

            migrationBuilder.AddColumn<int>(
                name: "ForceServerId",
                table: "Rules",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rules_ForceServerId",
                table: "Rules",
                column: "ForceServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_Servers_ForceServerId",
                table: "Rules",
                column: "ForceServerId",
                principalTable: "Servers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rules_Servers_ForceServerId",
                table: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_Rules_ForceServerId",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "ForceServerId",
                table: "Rules");

            migrationBuilder.AddColumn<string>(
                name: "ForceUpstream",
                table: "Rules",
                type: "TEXT",
                nullable: true);
        }
    }
}
