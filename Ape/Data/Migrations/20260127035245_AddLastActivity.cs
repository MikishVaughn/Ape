using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ape.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivity",
                table: "UserProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastActivity",
                table: "UserProfiles");
        }
    }
}
