using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gitbers.Migrations
{
    /// <inheritdoc />
    public partial class MakeTeamMemberUserOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViberConnected",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "ViberConnectionCode",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "ViberConnectionCodeExpiresAt",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "ViberUserId",
                table: "TeamMembers");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "TeamMembers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "TeamMembers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ViberConnected",
                table: "TeamMembers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddColumn<string>(
                name: "ViberUserId",
                table: "TeamMembers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
