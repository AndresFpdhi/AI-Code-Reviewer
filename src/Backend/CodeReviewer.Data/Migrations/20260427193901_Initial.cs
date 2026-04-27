using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeReviewer.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepoOwner = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    RepoName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PrNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    HeadSha = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    PrTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    PrUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    RawJson = table.Column<string>(type: "TEXT", nullable: false),
                    CommentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CreatedAt",
                table: "Reviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RepoOwner_RepoName_PrNumber",
                table: "Reviews",
                columns: new[] { "RepoOwner", "RepoName", "PrNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");
        }
    }
}
