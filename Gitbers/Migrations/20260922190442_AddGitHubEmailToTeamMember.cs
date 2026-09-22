using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gitbers.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubEmailToTeamMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitHubEmail",
                table: "TeamMembers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubEmail",
                table: "TeamMembers");
        }
    }
}
