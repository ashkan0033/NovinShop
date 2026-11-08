using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovinShop.Migrations
{
    /// <inheritdoc />
    public partial class AddSeo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlugRedirects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    OldSlug = table.Column<string>(type: "TEXT", nullable: false),
                    NewSlug = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlugRedirects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlugRedirects_Type_OldSlug",
                table: "SlugRedirects",
                columns: new[] { "Type", "OldSlug" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlugRedirects");
        }
    }
}
