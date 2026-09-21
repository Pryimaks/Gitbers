using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gitbers.Migrations
{
    /// <inheritdoc />
    public partial class AddViberConnectionCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ViberConnectionCode",
                table: "TeamMembers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ViberConnectionCodeExpiresAt",
                table: "TeamMembers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViberConnectionCode",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "ViberConnectionCodeExpiresAt",
                table: "TeamMembers");
        }
    }
}
