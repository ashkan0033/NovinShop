using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovinShop.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentGateways : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Authority",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptImagePath",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNote",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentGateways",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSandbox = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    MerchantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CardNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    CardHolder = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    ShebaNumber = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGateways", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGateways_Key",
                table: "PaymentGateways",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentGateways");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Authority",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReceiptImagePath",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReceiptNote",
                table: "Orders");
        }
    }
}
