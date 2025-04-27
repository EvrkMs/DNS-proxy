using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnsProxy.Migrations
{
    /// <inheritdoc />
    public partial class NewStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Rcode",
                table: "Stats",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rcode",
                table: "Stats");
        }
    }
}
