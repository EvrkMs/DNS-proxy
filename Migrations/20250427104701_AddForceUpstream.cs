using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnsProxy.Migrations
{
    /// <inheritdoc />
    public partial class AddForceUpstream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ForceUpstream",
                table: "Rules",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForceUpstream",
                table: "Rules");
        }
    }
}
