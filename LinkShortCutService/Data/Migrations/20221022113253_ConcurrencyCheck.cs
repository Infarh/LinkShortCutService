using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkShortCutService.Data.Migrations
{
    public partial class ConcurrencyCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Links",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Links");
        }
    }
}
