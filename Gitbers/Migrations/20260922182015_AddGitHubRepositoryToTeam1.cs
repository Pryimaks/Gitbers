using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gitbers.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubRepositoryToTeam1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitHubOwner",
                table: "Teams",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GitHubRepository",
                table: "Teams",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubOwner",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "GitHubRepository",
                table: "Teams");
        }
    }
}
