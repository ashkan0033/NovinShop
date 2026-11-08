using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovinShop.Migrations
{
    /// <inheritdoc />
    public partial class AddProductReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: false),
                    Pros = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Cons = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    IsBuyer = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    AdminReply = table.Column<string>(type: "TEXT", maxLength: 600, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ProductId_IsApproved",
                table: "ProductReviews",
                columns: new[] { "ProductId", "IsApproved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductReviews");
        }
    }
}
