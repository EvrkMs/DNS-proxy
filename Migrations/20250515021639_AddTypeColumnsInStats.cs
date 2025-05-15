using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnsProxy.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeColumnsInStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ushort>(
                name: "Type",
                table: "Stats",
                type: "INTEGER",
                nullable: false,
                defaultValue: (ushort)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Stats");
        }
    }
}
